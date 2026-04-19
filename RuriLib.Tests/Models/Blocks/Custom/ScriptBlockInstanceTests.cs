using System;
using System.Collections.Generic;
using RuriLib.Exceptions;
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

    private static ScriptBlockInstance CreateBlock()
        => new(new ScriptBlockDescriptor());
}
