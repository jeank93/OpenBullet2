using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RuriLib.Exceptions;
using RuriLib.Helpers.CSharp;
using RuriLib.Models.Blocks.Custom;
using RuriLib.Models.Blocks.Custom.Script;
using RuriLib.Models.Configs;
using RuriLib.Models.Variables;
using Xunit;

namespace RuriLib.Tests.Models.Blocks.Custom;

public class ScriptBlockInstanceTests
{
    private readonly string _nl = Environment.NewLine;

    [Fact]
    public void ToLC_WritesScriptBlockFormat()
    {
        var block = CreateBlock();

        var expected = $"INTERPRETER:Jint{_nl}INPUT x,y{_nl}BEGIN SCRIPT{_nl}var result = x + y;{_nl}END SCRIPT{_nl}OUTPUT Int @result{_nl}";
        Assert.Equal(expected, block.ToLC());
    }

    [Fact]
    public void FromLC_ParsesScriptBlockFormat()
    {
        var block = CreateBlock();
        var script = $"INTERPRETER:NodeJS{_nl}INPUT input.DATA, x{_nl}BEGIN SCRIPT{_nl}var result = x + 1;{_nl}END SCRIPT{_nl}OUTPUT String @result";
        var lineNumber = 0;

        block.FromLC(ref script, ref lineNumber);

        Assert.Equal(Interpreter.NodeJS, block.Interpreter);
        Assert.Equal("input.DATA, x", block.InputVariables);
        Assert.Equal("var result = x + 1;", block.Script);
        Assert.Single(block.OutputVariables);
        Assert.Equal(VariableType.String, block.OutputVariables[0].Type);
        Assert.Equal("result", block.OutputVariables[0].Name);
        Assert.Equal(6, lineNumber);
    }

    [Fact]
    public void FromLC_MissingEndScript_Throws()
    {
        var block = CreateBlock();
        var script = $"INTERPRETER:Jint{_nl}INPUT x,y{_nl}BEGIN SCRIPT{_nl}var result = x + y;";
        var lineNumber = 0;

        Assert.Throws<LoliCodeParsingException>(() => block.FromLC(ref script, ref lineNumber));
    }

    [Fact]
    public void ToCSharp_NodeJs_DeclaresOutputsAndSanitizesInputs()
    {
        var block = CreateBlock();
        block.Interpreter = Interpreter.NodeJS;
        block.InputVariables = "input.DATA, x";
        block.Script = "var result = DATA + x;";
        block.OutputVariables =
        [
            new OutputVariable { Name = "result", Type = VariableType.String }
        ];

        var definedVariables = new List<string>();
        var output = block.ToCSharp(definedVariables, new ConfigSettings());

        Assert.Contains("module.exports = async (DATA,x) => {", output);
        Assert.Contains("new object[] { input.DATA, x }", output);
        Assert.Contains("string result = ", output);
        Assert.Contains("result = tmp_", output);
        Assert.Contains("data.LogVariableAssignment(nameof(result));", output);
        Assert.Contains("result", definedVariables);
    }

    [Fact]
    public void ToCSharp_NodeJs_ReusesExistingOutputVariable()
    {
        var block = CreateBlock();
        block.Interpreter = Interpreter.NodeJS;

        var output = block.ToCSharp(["result"], new ConfigSettings());

        Assert.DoesNotContain("string result =", output);
        Assert.Contains("result = tmp_", output);
    }

    [Fact]
    public void ToSyntax_NodeJs_MatchesNormalizedLegacyOutput()
    {
        var block = CreateBlock();
        block.Interpreter = Interpreter.NodeJS;
        block.InputVariables = "input.DATA, x";
        block.Script = "var result = DATA + x;";
        block.OutputVariables =
        [
            new OutputVariable { Name = "result", Type = VariableType.String }
        ];

        var legacy = NormalizeTempNames(
            StatementSyntaxParser.ParseStatements(block.ToCSharp([], new ConfigSettings())).ToSnippet());
        var syntax = NormalizeTempNames(
            block.ToSyntax(new BlockSyntaxGenerationContext([], new ConfigSettings())).ToSnippet());

        Assert.Equal(legacy, syntax);
    }

    [Fact]
    public void ToSyntax_ParityMatrix_MatchesLegacyAndVariableTracking()
    {
        AssertParity(CreateNodeJsBlock(), []);
        AssertParity(CreateNodeJsBlock(), ["result"]);
        AssertParity(CreateJintBlock(), []);
        AssertParity(CreateIronPythonBlock(), []);
    }

    private static ScriptBlockInstance CreateBlock()
        => new(new ScriptBlockDescriptor());

    private static ScriptBlockInstance CreateNodeJsBlock()
        => new(new ScriptBlockDescriptor())
        {
            Interpreter = Interpreter.NodeJS,
            InputVariables = "input.DATA, x",
            Script = "var result = DATA + x;",
            OutputVariables =
            [
                new OutputVariable { Name = "result", Type = VariableType.String }
            ]
        };

    private static ScriptBlockInstance CreateJintBlock()
        => new(new ScriptBlockDescriptor())
        {
            Interpreter = Interpreter.Jint,
            InputVariables = "globals.source, y",
            Script = "var count = source.length + y;",
            OutputVariables =
            [
                new OutputVariable { Name = "count", Type = VariableType.Int }
            ]
        };

    private static ScriptBlockInstance CreateIronPythonBlock()
        => new(new ScriptBlockDescriptor())
        {
            Interpreter = Interpreter.IronPython,
            InputVariables = "input.NAME",
            Script = "message = NAME + '_done'",
            OutputVariables =
            [
                new OutputVariable { Name = "message", Type = VariableType.String }
            ]
        };

    private static void AssertParity(ScriptBlockInstance block, List<string> definedVariables)
    {
        var legacyVariables = new List<string>(definedVariables);
        var syntaxVariables = new List<string>(definedVariables);
        var settings = new ConfigSettings();

        var legacy = NormalizeTempNames(
            StatementSyntaxParser.ParseStatements(block.ToCSharp(legacyVariables, settings)).ToSnippet());
        var syntax = NormalizeTempNames(
            block.ToSyntax(new BlockSyntaxGenerationContext(syntaxVariables, settings)).ToSnippet());

        Assert.Equal(legacy, syntax);
        Assert.Equal(legacyVariables, syntaxVariables);
    }

    private static string NormalizeTempNames(string input)
        => Regex.Replace(
            Regex.Replace(input.Replace("\\r\\n", "\\n"), "\"[a-f0-9]{32}\"", "\"HASH\""),
            @"tmp_[A-Za-z0-9_]+",
            "tmp_TEMP");
}
