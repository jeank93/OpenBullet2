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
using RuriLib.Helpers.LoliCode;
using RuriLib.Models.Blocks.Custom.Script;
using RuriLib.Models.Configs;
using RuriLib.Models.Variables;

namespace RuriLib.Models.Blocks.Custom;

public class ScriptBlockInstance : BlockInstance
{
    public string Script { get; set; } = "var result = x + y;";

    public List<OutputVariable> OutputVariables { get; set; } =
    [
        new OutputVariable
        {
            Name = "result",
            Type = VariableType.Int
        }
    ];

    public string InputVariables { get; set; } = "x,y";
    public Interpreter Interpreter { get; set; } = Interpreter.Jint;

    public ScriptBlockInstance(ScriptBlockDescriptor descriptor)
        : base(descriptor)
    {
    }

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
}
