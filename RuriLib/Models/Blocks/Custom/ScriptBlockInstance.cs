using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RuriLib.Exceptions;
using RuriLib.Extensions;
using RuriLib.Functions.Conversion;
using RuriLib.Functions.Crypto;
using RuriLib.Helpers;
using RuriLib.Helpers.CSharp;
using RuriLib.Helpers.LoliCode;
using RuriLib.Models.Blocks.Custom.Script;
using RuriLib.Models.Configs;
using RuriLib.Models.Variables;

namespace RuriLib.Models.Blocks.Custom;

/// <summary>
/// Block instance for invoking a script in another language.
/// </summary>
public class ScriptBlockInstance : BlockInstance
{
    /// <summary>
    /// Gets or sets the script body.
    /// </summary>
    public string Script { get; set; } = "var result = x + y;";

    /// <summary>
    /// Gets or sets the output variables exposed by the script.
    /// </summary>
    public List<OutputVariable> OutputVariables { get; set; } =
    [
        new()
        {
            Name = "result",
            Type = VariableType.Int
        }
    ];

    /// <summary>
    /// Gets or sets the comma-separated input variable list.
    /// </summary>
    public string InputVariables { get; set; } = "x,y";
    /// <summary>
    /// Gets or sets the interpreter used to run the script.
    /// </summary>
    public Interpreter Interpreter { get; set; } = Interpreter.Jint;

    /// <summary>
    /// Initializes a new <see cref="ScriptBlockInstance"/>.
    /// </summary>
    /// <param name="descriptor">The block descriptor.</param>
    public ScriptBlockInstance(ScriptBlockDescriptor descriptor)
        : base(descriptor)
    {
    }

    /// <inheritdoc />
    public override string ToLC(bool printDefaultParams = false)
    {
        /*
         *   INTERPRETER:Jint
         *   INPUT x,y
         *   BEGIN SCRIPT
         *   var result = x + y;
         *   END SCRIPT
         *   OUTPUT Int result
         */

        using var writer = new LoliCodeWriter(base.ToLC(printDefaultParams));
        writer.WriteLine($"INTERPRETER:{Interpreter}");
        writer.WriteLine($"INPUT {InputVariables}");
        writer.WriteLine("BEGIN SCRIPT");
        writer.WriteLine(TrimTrailingLineEndings(Script));
        writer.WriteLine("END SCRIPT");

        foreach (var output in OutputVariables)
        {
            writer.WriteLine($"OUTPUT {output.Type} @{output.Name}");
        }

        return writer.ToString();
    }

    /// <inheritdoc />
    public override void FromLC(ref string script, ref int lineNumber)
    {
        ArgumentNullException.ThrowIfNull(script);

        // First parse the options that are common to every BlockInstance
        base.FromLC(ref script, ref lineNumber);

        using var reader = new StringReader(script);
        using var writer = new StringWriter();

        var interpreterLine = reader.ReadLine();
        lineNumber++;

        if (interpreterLine is null)
        {
            throw new LoliCodeParsingException(lineNumber, "Missing interpreter definition");
        }

        try
        {
            Interpreter = Enum.Parse<Interpreter>(
                Regex.Match(interpreterLine, "INTERPRETER:([^ ]+)$").Groups[1].Value);
        }
        catch
        {
            throw new LoliCodeParsingException(lineNumber,
                $"Invalid interpreter definition: {interpreterLine.TruncatePretty(50)}");
        }

        var inputVariablesLine = reader.ReadLine();
        lineNumber++;

        if (inputVariablesLine is null)
        {
            throw new LoliCodeParsingException(lineNumber, "Missing input variables definition");
        }

        try
        {
            InputVariables = Regex.Match(inputVariablesLine, "INPUT (.*)$").Groups[1].Value;
        }
        catch
        {
            throw new LoliCodeParsingException(lineNumber, "Invalid input variables definition");
        }

        var beginScriptLine = reader.ReadLine();
        lineNumber++;

        if (beginScriptLine != "BEGIN SCRIPT")
        {
            throw new LoliCodeParsingException(lineNumber,
                $"Invalid script start definition: {beginScriptLine?.TruncatePretty(50) ?? "<null>"}");
        }

        var foundEndScript = false;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;

            if (line == "END SCRIPT")
            {
                foundEndScript = true;
                break;
            }

            writer.WriteLine(line);
        }

        if (!foundEndScript)
        {
            throw new LoliCodeParsingException(lineNumber, "Missing END SCRIPT definition");
        }

        Script = TrimTrailingLineEndings(writer.ToString()); // Remove blank lines at the end

        OutputVariables = [];
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            var match = Regex.Match(line, "OUTPUT ([^ ]+) @([^ ]+)$");

            try
            {
                OutputVariables.Add(new OutputVariable
                {
                    Type = Enum.Parse<VariableType>(match.Groups[1].Value),
                    Name = match.Groups[2].Value
                });
            }
            catch
            {
                // TODO: Warn the user that the output variable is invalid
            }
        }
    }

    /// <inheritdoc />
    public override string ToCSharp(List<string> definedVariables, ConfigSettings settings)
    {
        ArgumentNullException.ThrowIfNull(definedVariables);
        ArgumentNullException.ThrowIfNull(settings);

        using var writer = new StringWriter();
        var resultName = "tmp_" + VariableNames.RandomName(6);
        var engineName = "tmp_" + VariableNames.RandomName(6);
        var scopeName = "tmp_" + VariableNames.RandomName(6);

        switch (Interpreter)
        {
            case Interpreter.Jint:
                var scriptHash = HexConverter.ToHexString(Crypto.MD5(Encoding.UTF8.GetBytes(Script)));
                var scriptPath = $"Scripts/{scriptHash}.{GetScriptFileExtension(Interpreter)}";

                if (!Directory.Exists("Scripts"))
                {
                    Directory.CreateDirectory("Scripts");
                }

                if (!File.Exists(scriptPath))
                {
                    File.WriteAllText(scriptPath, Script);
                }

                writer.WriteLine($"var {engineName} = new Engine();");

                foreach (var input in GetInputs())
                {
                    writer.WriteLine($"{engineName}.SetValue(nameof({input}), {input});");
                }

                writer.WriteLine($"{engineName} = InvokeJint(data, {engineName}, \"{scriptPath}\");");

                foreach (var output in OutputVariables)
                {
                    if (!definedVariables.Contains(output.Name))
                    {
                        writer.Write($"{ToCSharpType(output.Type)} ");
                        definedVariables.Add(output.Name);
                    }

                    writer.WriteLine($"{output.Name} = {engineName}.Global.GetProperty(\"{output.Name}\").Value.{GetJintMethod(output.Type)};");
                }

                break;

            case Interpreter.NodeJS:
                var nodeScript = $$"""
                    module.exports = async ({{MakeInputs()}}) => {
                        {{Script}}
                        var noderesult = {
                        {{MakeNodeObject()}}
                        };
                        return noderesult;
                    }
                    """;

                var nodeScriptHash = HexConverter.ToHexString(Crypto.MD5(Encoding.UTF8.GetBytes(nodeScript)));
                var escapedScript = JsonConvert.ToString(nodeScript);

                writer.WriteLine($"var {resultName} = await InvokeNode<dynamic>(data, {escapedScript}, new object[] {{ {InputVariables} }}, true, \"{nodeScriptHash}\");");

                foreach (var output in OutputVariables)
                {
                    if (!definedVariables.Contains(output.Name))
                    {
                        writer.Write($"{ToCSharpType(output.Type)} ");
                        definedVariables.Add(output.Name);
                    }

                    writer.WriteLine($"{output.Name} = {GetNodeMethod(resultName, output)};");
                }

                break;

            case Interpreter.IronPython:
                scriptHash = HexConverter.ToHexString(Crypto.MD5(Encoding.UTF8.GetBytes(Script)));
                scriptPath = $"Scripts/{scriptHash}.{GetScriptFileExtension(Interpreter)}";

                if (!Directory.Exists("Scripts"))
                {
                    Directory.CreateDirectory("Scripts");
                }

                if (!File.Exists(scriptPath))
                {
                    File.WriteAllText(scriptPath, Script);
                }

                writer.WriteLine($"var {scopeName} = GetIronPyScope(data);");

                foreach (var input in GetInputs())
                {
                    writer.WriteLine($"{scopeName}.SetVariable(nameof({input}), {input});");
                }

                writer.WriteLine($"ExecuteIronPyScript(data, {scopeName}, \"{scriptPath}\");");

                foreach (var output in OutputVariables)
                {
                    if (!definedVariables.Contains(output.Name))
                    {
                        writer.Write($"{ToCSharpType(output.Type)} ");
                        definedVariables.Add(output.Name);
                    }

                    writer.WriteLine($"{output.Name} = {scopeName}" + output.Type switch
                    {
                        VariableType.ListOfStrings => $".GetVariable<IList<object>>(\"{output.Name}\").Cast<string>().ToList();",
                        VariableType.ByteArray => $".GetVariable<IList<object>>(\"{output.Name}\").Cast<byte>().ToArray();",
                        _ => $".GetVariable<{ToCSharpType(output.Type)}>(\"{output.Name}\");"
                    });
                }

                break;
        }

        foreach (var output in OutputVariables)
        {
            writer.WriteLine($"data.LogVariableAssignment(nameof({output.Name}));");
        }

        return writer.ToString();
    }

    /// <inheritdoc />
    public override IEnumerable<StatementSyntax> ToSyntax(BlockSyntaxGenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var statements = new List<StatementSyntax>();
        var resultName = "tmp_" + VariableNames.RandomName(6);
        var engineName = "tmp_" + VariableNames.RandomName(6);
        var scopeName = "tmp_" + VariableNames.RandomName(6);

        switch (Interpreter)
        {
            case Interpreter.Jint:
                var scriptPath = EnsureScriptFile(Script, Interpreter);

                statements.Add(BlockSyntaxFactory.CreateVariableDeclaration(
                    "var",
                    engineName,
                    SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("Engine"))
                        .WithArgumentList(SyntaxFactory.ArgumentList())));

                foreach (var input in GetInputs())
                {
                    statements.Add(SyntaxFactory.ExpressionStatement(
                        BlockSyntaxFactory.CreateMemberInvocation(
                            SyntaxFactory.IdentifierName(engineName),
                            "SetValue",
                            SyntaxFactory.Argument(BlockSyntaxFactory.CreateNameofExpression(input)),
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression(input)))));
                }

                statements.Add(BlockSyntaxFactory.CreateAssignment(
                    engineName,
                    BlockSyntaxFactory.CreateInvocation(
                        SyntaxFactory.IdentifierName("InvokeJint"),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("data")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(engineName)),
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(scriptPath))))));

                foreach (var output in OutputVariables)
                {
                    statements.Add(CreateOutputAssignmentStatement(
                        context.DefinedVariables,
                        output,
                        BuildJintOutputExpression(engineName, output)));
                }

                break;

            case Interpreter.NodeJS:
                var nodeScript = $$"""
                    module.exports = async ({{MakeInputs()}}) => {
                        {{Script}}
                        var noderesult = {
                        {{MakeNodeObject()}}
                        };
                        return noderesult;
                    }
                    """;

                var nodeScriptHash = HexConverter.ToHexString(Crypto.MD5(Encoding.UTF8.GetBytes(nodeScript)));
                var escapedScript = JsonConvert.ToString(nodeScript);
                var nodeInvocation = BlockSyntaxFactory.CreateInvocation(
                    SyntaxFactory.ParseExpression("InvokeNode<dynamic>"),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("data")),
                    SyntaxFactory.Argument(SyntaxFactory.ParseExpression(escapedScript)),
                    SyntaxFactory.Argument(BuildInputArrayExpression()),
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(nodeScriptHash))));

                statements.Add(BlockSyntaxFactory.CreateVariableDeclaration(
                    "var",
                    resultName,
                    SyntaxFactory.AwaitExpression(nodeInvocation)));

                foreach (var output in OutputVariables)
                {
                    statements.Add(CreateOutputAssignmentStatement(
                        context.DefinedVariables,
                        output,
                        BuildNodeOutputExpression(resultName, output)));
                }

                break;

            case Interpreter.IronPython:
                scriptPath = EnsureScriptFile(Script, Interpreter);

                statements.Add(BlockSyntaxFactory.CreateVariableDeclaration(
                    "var",
                    scopeName,
                    BlockSyntaxFactory.CreateInvocation(
                        SyntaxFactory.IdentifierName("GetIronPyScope"),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("data")))));

                foreach (var input in GetInputs())
                {
                    statements.Add(SyntaxFactory.ExpressionStatement(
                        BlockSyntaxFactory.CreateMemberInvocation(
                            SyntaxFactory.IdentifierName(scopeName),
                            "SetVariable",
                            SyntaxFactory.Argument(BlockSyntaxFactory.CreateNameofExpression(input)),
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression(input)))));
                }

                statements.Add(SyntaxFactory.ExpressionStatement(
                    BlockSyntaxFactory.CreateInvocation(
                        SyntaxFactory.IdentifierName("ExecuteIronPyScript"),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("data")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(scopeName)),
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(scriptPath))))));

                foreach (var output in OutputVariables)
                {
                    statements.Add(CreateOutputAssignmentStatement(
                        context.DefinedVariables,
                        output,
                        BuildIronPythonOutputExpression(scopeName, output)));
                }

                break;
        }

        foreach (var output in OutputVariables)
        {
            statements.Add(BlockSyntaxFactory.CreateDataMethodWithNameofArgument("LogVariableAssignment", output.Name));
        }

        return statements;
    }

    private ExpressionSyntax BuildNodeOutputExpression(string resultName, OutputVariable output)
        => SyntaxFactory.ParseExpression(GetNodeMethod(resultName, output));

    private string GetNodeMethod(string resultName, OutputVariable output)
        => output.Type switch
        {
            VariableType.Bool => $"{resultName}.GetProperty(\"{output.Name}\").GetBoolean()",
            VariableType.ByteArray => $"{resultName}.GetProperty(\"{output.Name}\").GetBytesFromBase64()",
            VariableType.Float => $"{resultName}.GetProperty(\"{output.Name}\").GetSingle()",
            VariableType.Int => $"{resultName}.GetProperty(\"{output.Name}\").GetInt32()",
            VariableType.String => $"{resultName}.GetProperty(\"{output.Name}\").ToString()",
            VariableType.ListOfStrings => $"((System.Text.Json.JsonElement.ArrayEnumerator){resultName}.GetProperty(\"{output.Name}\").EnumerateArray()).Select(e => e.GetString()).ToList()",
            VariableType.DictionaryOfStrings => $"((System.Text.Json.JsonElement.ObjectEnumerator){resultName}.GetProperty(\"{output.Name}\").EnumerateObject()).ToDictionary(e => e.Name, e => e.Value.GetString())",
            _ => throw new NotImplementedException()
        };

    private string GetJintMethod(VariableType type)
        => type switch
        {
            VariableType.Bool => "AsBoolean()",
            VariableType.ByteArray => "TryCast<byte[]>()",
            VariableType.Float => "AsNumber().ToSingle()",
            VariableType.Int => "AsNumber().ToInt()",
            VariableType.ListOfStrings => "AsArray().GetEnumerator().ToEnumerable().Select(j => j.ToString()).ToList()",
            VariableType.String => "ToString()",
            _ => throw new NotImplementedException() // Dictionary not implemented yet
        };

    private ExpressionSyntax BuildJintOutputExpression(string engineName, OutputVariable output)
        => SyntaxFactory.ParseExpression(
            $"{engineName}.Global.GetProperty(\"{output.Name}\").Value.{GetJintMethod(output.Type)}");

    private ExpressionSyntax BuildIronPythonOutputExpression(string scopeName, OutputVariable output)
        => SyntaxFactory.ParseExpression(output.Type switch
        {
            VariableType.ListOfStrings => $"{scopeName}.GetVariable<IList<object>>(\"{output.Name}\").Cast<string>().ToList()",
            VariableType.ByteArray => $"{scopeName}.GetVariable<IList<object>>(\"{output.Name}\").Cast<byte>().ToArray()",
            _ => $"{scopeName}.GetVariable<{ToCSharpType(output.Type)}>(\"{output.Name}\")"
        });

    private string ToCSharpType(VariableType type)
        => type switch
        {
            VariableType.Bool => "bool",
            VariableType.ByteArray => "byte[]",
            VariableType.Float => "float",
            VariableType.Int => "int",
            VariableType.ListOfStrings => "List<string>",
            VariableType.String => "string",
            VariableType.DictionaryOfStrings => "Dictionary<string, string>",
            _ => throw new NotImplementedException()
        };

    private string GetScriptFileExtension(Interpreter interpreter)
        => interpreter switch
        {
            Interpreter.Jint => "js",
            Interpreter.NodeJS => "js",
            Interpreter.IronPython => "py",
            _ => throw new NotImplementedException()
        };

    private string MakeNodeObject()
        => string.Join(global::System.Environment.NewLine, OutputVariables.Select(o => $"  '{o.Name}': {o.Name},"));

    private static string TrimTrailingLineEndings(string value)
        => value.TrimEnd('\r', '\n');

    private string MakeInputs()
        => string.Join(",", GetInputs().Select(SanitizeInput));

    private IEnumerable<string> GetInputs()
        => string.IsNullOrWhiteSpace(InputVariables)
            ? []
            : InputVariables.Split(',')
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrWhiteSpace(i));

    // Converts input.DATA into DATA
    private string SanitizeInput(string input)
        => Regex.Match(input, "[A-Za-z0-9_]+$").Value;

    private static string EnsureScriptFile(string script, Interpreter interpreter)
    {
        var scriptHash = HexConverter.ToHexString(Crypto.MD5(Encoding.UTF8.GetBytes(script)));
        var scriptPath = $"Scripts/{scriptHash}.{interpreter switch
        {
            Interpreter.Jint => "js",
            Interpreter.NodeJS => "js",
            Interpreter.IronPython => "py",
            _ => throw new NotImplementedException()
        }}";

        if (!Directory.Exists("Scripts"))
        {
            Directory.CreateDirectory("Scripts");
        }

        if (!File.Exists(scriptPath))
        {
            File.WriteAllText(scriptPath, script);
        }

        return scriptPath;
    }

    private ExpressionSyntax BuildInputArrayExpression()
    {
        var inputs = GetInputs()
            .Select(input => SyntaxFactory.ParseExpression(input))
            .ToArray();

        return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression())))))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(inputs)));
    }

    private StatementSyntax CreateOutputAssignmentStatement(
        List<string> definedVariables,
        OutputVariable output,
        ExpressionSyntax expression)
    {
        var assignToExistingVariable = definedVariables.Contains(output.Name);

        if (!assignToExistingVariable)
        {
            definedVariables.Add(output.Name);
        }

        return BlockSyntaxFactory.CreateVariableDeclarationOrAssignment(
            ToCSharpType(output.Type),
            output.Name,
            expression,
            assignToExistingVariable);
    }
}
