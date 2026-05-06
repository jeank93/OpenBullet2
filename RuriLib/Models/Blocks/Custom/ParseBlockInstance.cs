using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RuriLib.Exceptions;
using RuriLib.Extensions;
using RuriLib.Helpers;
using RuriLib.Helpers.CSharp;
using RuriLib.Helpers.LoliCode;
using RuriLib.Models.Blocks.Custom.Parse;
using RuriLib.Models.Configs;

namespace RuriLib.Models.Blocks.Custom;

/// <summary>
/// Block instance for the custom parse block.
/// </summary>
public class ParseBlockInstance(ParseBlockDescriptor descriptor) : BlockInstance(descriptor)
{
    private string outputVariable = "parseOutput";

    /// <summary>
    /// Gets or sets the output variable written by the block.
    /// </summary>
    public string OutputVariable
    {
        get => outputVariable;
        set => outputVariable = VariableNames.MakeValid(value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether multiple results should be returned.
    /// </summary>
    public bool Recursive { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the output should be captured.
    /// </summary>
    public bool IsCapture { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether safe mode is enabled.
    /// </summary>
    public bool Safe { get; set; }
    /// <summary>
    /// Gets or sets the parsing mode.
    /// </summary>
    public ParseMode Mode { get; set; } = ParseMode.LR;

    /// <inheritdoc />
    public override string ToLC(bool printDefaultParams = false)
    {
        /*
         *   recursive = True
         *   mode = LR
         *   input = "hello how are you"
         *   leftDelim = "hello"
         *   rightDelim = "you"
         *   caseSensitive = True
         *   => CAP PARSED
         */

        using var writer = new LoliCodeWriter(base.ToLC(printDefaultParams));

        if (Safe)
        {
            writer.AppendLine("SAFE", 2);
        }

        if (Recursive)
        {
            writer.AppendLine("RECURSIVE", 2);
        }

        writer.AppendLine($"MODE:{Mode}", 2);

        var isCap = IsCapture ? "CAP" : "VAR";
        writer.AppendLine($"=> {isCap} @{OutputVariable}", 2);

        return writer.ToString();
    }

    /// <inheritdoc />
    public override void FromLC(ref string script, ref int lineNumber)
    {
        /*
         *   recursive = True
         *   mode = LR
         *   input = "hello how are you"
         *   leftDelim = "hello"
         *   rightDelim = "you"
         *   caseSensitive = True
         *   => CAP PARSED
         */

        ArgumentNullException.ThrowIfNull(script);

        // First parse the options that are common to every BlockInstance
        base.FromLC(ref script, ref lineNumber);

        using var reader = new StringReader(script);

        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            lineNumber++;
            var lineCopy = line;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("SAFE", StringComparison.Ordinal))
            {
                Safe = true;
                continue;
            }

            if (line.StartsWith("RECURSIVE", StringComparison.Ordinal))
            {
                Recursive = true;
            }
            else if (line.StartsWith("MODE", StringComparison.Ordinal))
            {
                try
                {
                    Mode = Enum.Parse<ParseMode>(Regex.Match(line, "MODE:([A-Za-z]+)").Groups[1].Value);
                }
                catch
                {
                    throw new LoliCodeParsingException(lineNumber, $"Could not understand the parsing mode: {lineCopy.TruncatePretty(50)}");
                }
            }
            else if (line.StartsWith("=>", StringComparison.Ordinal))
            {
                try
                {
                    var match = Regex.Match(line, "^=> ([A-Za-z]{3}) @(.+)$");

                    if (!match.Success)
                    {
                        throw new FormatException();
                    }

                    IsCapture = match.Groups[1].Value.Equals("CAP", StringComparison.OrdinalIgnoreCase);
                    OutputVariable = match.Groups[2].Value.Trim();
                }
                catch
                {
                    throw new LoliCodeParsingException(lineNumber, $"The output variable declaration is in the wrong format: {lineCopy.TruncatePretty(50)}");
                }
            }
            else
            {
                try
                {
                    LoliCodeParser.ParseSetting(ref line, Settings, Descriptor);
                }
                catch
                {
                    throw new LoliCodeParsingException(lineNumber, $"Could not parse the setting: {lineCopy.TruncatePretty(50)}");
                }
            }
        }
    }

    /// <inheritdoc />
    public override string ToCSharp(List<string> definedVariables, ConfigSettings settings)
    {
        ArgumentNullException.ThrowIfNull(definedVariables);
        ArgumentNullException.ThrowIfNull(settings);

        using var writer = new StringWriter();
        var outputType = Recursive ? "List<string>" : "string";
        var defaultReturnValue = Recursive ? "new List<string>()" : "string.Empty";

        // Safe mode, wrap method in try/catch but declare variable outside of it
        if (Safe)
        {
            // Only do this if we haven't declared the variable yet!
            if (!definedVariables.Contains(OutputVariable) && !OutputVariable.StartsWith("globals.", StringComparison.Ordinal))
            {
                if (!Disabled)
                {
                    definedVariables.Add(OutputVariable);
                }

                writer.WriteLine($"{outputType} {OutputVariable} = {defaultReturnValue};");
            }

            writer.WriteLine("try {");

            // Here we already know the variable exists so we just do the assignment
            writer.Write($"{OutputVariable} = ");

            WriteParseMethod(writer);

            writer.WriteLine("} catch (Exception safeException) {");
            writer.WriteLine("data.ERROR = safeException.PrettyPrint();");
            writer.WriteLine("data.Logger.Log($\"[SAFE MODE] Exception caught and saved to data.ERROR: {data.ERROR}\", LogColors.Tomato); }");
        }
        else
        {
            if (definedVariables.Contains(OutputVariable) || OutputVariable.StartsWith("globals.", StringComparison.Ordinal))
            {
                writer.Write($"{OutputVariable} = ");
            }
            else
            {
                if (!Disabled)
                {
                    definedVariables.Add(OutputVariable);
                }

                writer.Write($"{outputType} {OutputVariable} = ");
            }

            WriteParseMethod(writer);
        }

        return writer.ToString();
    }

    /// <inheritdoc />
    public override IEnumerable<StatementSyntax> ToSyntax(BlockSyntaxGenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var statements = new List<StatementSyntax>();
        var outputType = Recursive ? "List<string>" : "string";
        var defaultReturnValue = Recursive ? "new List<string>()" : "string.Empty";

        if (Safe)
        {
            if (!context.DefinedVariables.Contains(OutputVariable)
                && !OutputVariable.StartsWith("globals.", StringComparison.Ordinal))
            {
                if (!Disabled)
                {
                    context.DefinedVariables.Add(OutputVariable);
                }

                statements.Add(BlockSyntaxFactory.CreateVariableDeclaration(
                    outputType,
                    OutputVariable,
                    SyntaxFactory.ParseExpression(defaultReturnValue)));
            }

            statements.Add(SyntaxFactory.TryStatement(
                SyntaxFactory.Block(CreateExecutionStatements(context.DefinedVariables, true).ToArray()),
                SyntaxFactory.List([BlockSyntaxFactory.CreateSafeModeCatchClause()]),
                null));

            return statements;
        }

        statements.AddRange(CreateExecutionStatements(context.DefinedVariables, false));
        return statements;
    }

    private void WriteParseMethod(StringWriter writer)
    {
        switch (Mode)
        {
            case ParseMode.LR:
                writer.Write("ParseBetweenStrings");
                break;

            case ParseMode.CSS:
                writer.Write("QueryCssSelector");
                break;

            case ParseMode.XPath:
                writer.Write("QueryXPath");
                break;

            case ParseMode.Json:
                writer.Write("QueryJsonToken");
                break;

            case ParseMode.Regex:
                writer.Write("MatchRegexGroups");
                break;
        }

        if (Recursive)
        {
            writer.Write("Recursive");
        }

        writer.Write("(data, ");
        writer.Write(CSharpWriter.FromSetting(Settings["input"]) + ", ");

        switch (Mode)
        {
            case ParseMode.LR:
                writer.Write(CSharpWriter.FromSetting(Settings["leftDelim"]) + ", ");
                writer.Write(CSharpWriter.FromSetting(Settings["rightDelim"]) + ", ");
                writer.Write(CSharpWriter.FromSetting(Settings["caseSensitive"]) + ", ");
                break;

            case ParseMode.CSS:
                writer.Write(CSharpWriter.FromSetting(Settings["cssSelector"]) + ", ");
                writer.Write(CSharpWriter.FromSetting(Settings["attributeName"]) + ", ");
                break;

            case ParseMode.XPath:
                writer.Write(CSharpWriter.FromSetting(Settings["xPath"]) + ", ");
                writer.Write(CSharpWriter.FromSetting(Settings["attributeName"]) + ", ");
                break;

            case ParseMode.Json:
                writer.Write(CSharpWriter.FromSetting(Settings["jToken"]) + ", ");
                break;

            case ParseMode.Regex:
                writer.Write(CSharpWriter.FromSetting(Settings["pattern"]) + ", ");
                writer.Write(CSharpWriter.FromSetting(Settings["outputFormat"]) + ", ");
                writer.Write(CSharpWriter.FromSetting(Settings["multiLine"]) + ", ");
                break;
        }

        writer.Write(CSharpWriter.FromSetting(Settings["prefix"]) + ", ");
        writer.Write(CSharpWriter.FromSetting(Settings["suffix"]) + ", ");
        writer.Write(CSharpWriter.FromSetting(Settings["urlEncodeOutput"]));
        writer.WriteLine(");");

        writer.WriteLine($"data.LogVariableAssignment(nameof({OutputVariable}));");

        if (IsCapture)
        {
            writer.WriteLine($"data.MarkForCapture(nameof({OutputVariable}));");
        }
    }

    private List<StatementSyntax> CreateExecutionStatements(List<string> definedVariables, bool assignmentOnly)
    {
        var statements = new List<StatementSyntax>();
        var outputType = Recursive ? "List<string>" : "string";
        var invocation = BuildParseInvocationExpression();
        var assignToExistingVariable = assignmentOnly
            || definedVariables.Contains(OutputVariable)
            || OutputVariable.StartsWith("globals.", StringComparison.Ordinal);

        if (!assignToExistingVariable && !Disabled)
        {
            definedVariables.Add(OutputVariable);
        }

        statements.Add(BlockSyntaxFactory.CreateVariableDeclarationOrAssignment(
            outputType,
            OutputVariable,
            invocation,
            assignToExistingVariable));

        statements.Add(BlockSyntaxFactory.CreateDataMethodWithNameofArgument("LogVariableAssignment", OutputVariable));

        if (IsCapture)
        {
            statements.Add(BlockSyntaxFactory.CreateDataMethodWithNameofArgument("MarkForCapture", OutputVariable));
        }

        return statements;
    }

    private ExpressionSyntax BuildParseInvocationExpression()
    {
        var methodName = Mode switch
        {
            ParseMode.LR => "ParseBetweenStrings",
            ParseMode.CSS => "QueryCssSelector",
            ParseMode.XPath => "QueryXPath",
            ParseMode.Json => "QueryJsonToken",
            ParseMode.Regex => "MatchRegexGroups",
            _ => throw new NotSupportedException()
        };

        if (Recursive)
        {
            methodName += "Recursive";
        }

        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("data")),
            SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["input"]))
        };

        switch (Mode)
        {
            case ParseMode.LR:
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["leftDelim"])));
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["rightDelim"])));
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["caseSensitive"])));
                break;

            case ParseMode.CSS:
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["cssSelector"])));
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["attributeName"])));
                break;

            case ParseMode.XPath:
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["xPath"])));
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["attributeName"])));
                break;

            case ParseMode.Json:
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["jToken"])));
                break;

            case ParseMode.Regex:
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["pattern"])));
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["outputFormat"])));
                arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["multiLine"])));
                break;
        }

        arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["prefix"])));
        arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["suffix"])));
        arguments.Add(SyntaxFactory.Argument(CSharpWriter.FromSettingSyntax(Settings["urlEncodeOutput"])));

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(methodName),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }
}
