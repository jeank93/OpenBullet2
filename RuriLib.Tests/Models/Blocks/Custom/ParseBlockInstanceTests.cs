using System;
using System.Collections.Generic;
using RuriLib.Helpers.CSharp;
using RuriLib.Models.Blocks.Custom;
using RuriLib.Models.Blocks.Custom.Parse;
using RuriLib.Models.Blocks.Settings;
using RuriLib.Models.Configs;
using Xunit;

namespace RuriLib.Tests.Models.Blocks.Custom;

public class ParseBlockInstanceTests
{
    private readonly string _nl = Environment.NewLine;

    [Fact]
    public void ToLC_WritesParseBlockFormat()
    {
        var block = CreateBlock();
        block.Safe = true;
        block.Recursive = true;
        block.Mode = ParseMode.Regex;
        block.IsCapture = true;
        block.OutputVariable = "parsedOutput";

        var input = block.Settings["input"];
        input.InputMode = SettingInputMode.Variable;
        input.InputVariableName = "data.SOURCE";

        var pattern = block.Settings["pattern"];
        pattern.InputMode = SettingInputMode.Fixed;
        (pattern.FixedSetting as StringSetting)!.Value = "abc(.*)";

        var outputFormat = block.Settings["outputFormat"];
        outputFormat.InputMode = SettingInputMode.Fixed;
        (outputFormat.FixedSetting as StringSetting)!.Value = "$1";

        var multiLine = block.Settings["multiLine"];
        multiLine.InputMode = SettingInputMode.Fixed;
        (multiLine.FixedSetting as BoolSetting)!.Value = true;

        var expected = $"  input = @data.SOURCE{_nl}  pattern = \"abc(.*)\"{_nl}  outputFormat = \"$1\"{_nl}  multiLine = True{_nl}  SAFE{_nl}  RECURSIVE{_nl}  MODE:Regex{_nl}  => CAP @parsedOutput{_nl}";
        Assert.Equal(expected, block.ToLC());
    }

    [Fact]
    public void FromLC_ParsesParseBlockFormat()
    {
        var block = CreateBlock();
        var script = $"  input = @data.SOURCE{_nl}  leftDelim = \"hello\"{_nl}  rightDelim = \"you\"{_nl}  caseSensitive = False{_nl}  SAFE{_nl}  RECURSIVE{_nl}  MODE:LR{_nl}  => VAR @parsedOutput";
        var lineNumber = 0;

        block.FromLC(ref script, ref lineNumber);

        Assert.True(block.Safe);
        Assert.True(block.Recursive);
        Assert.False(block.IsCapture);
        Assert.Equal(ParseMode.LR, block.Mode);
        Assert.Equal("parsedOutput", block.OutputVariable);

        var input = block.Settings["input"];
        var leftDelim = block.Settings["leftDelim"];
        var rightDelim = block.Settings["rightDelim"];
        var caseSensitive = block.Settings["caseSensitive"];

        Assert.Equal(SettingInputMode.Variable, input.InputMode);
        Assert.Equal("data.SOURCE", input.InputVariableName);
        Assert.Equal("hello", (leftDelim.FixedSetting as StringSetting)!.Value);
        Assert.Equal("you", (rightDelim.FixedSetting as StringSetting)!.Value);
        Assert.False((caseSensitive.FixedSetting as BoolSetting)!.Value);
        Assert.Equal(8, lineNumber);
    }

    [Fact]
    public void ToCSharp_SafeRegexCapture_WritesExpectedCode()
    {
        var block = CreateBlock();
        block.Safe = true;
        block.Mode = ParseMode.Regex;
        block.IsCapture = true;
        block.OutputVariable = "parsedOutput";

        var input = block.Settings["input"];
        input.InputMode = SettingInputMode.Variable;
        input.InputVariableName = "data.SOURCE";

        var pattern = block.Settings["pattern"];
        pattern.InputMode = SettingInputMode.Fixed;
        (pattern.FixedSetting as StringSetting)!.Value = "abc(.*)";

        var outputFormat = block.Settings["outputFormat"];
        outputFormat.InputMode = SettingInputMode.Fixed;
        (outputFormat.FixedSetting as StringSetting)!.Value = "$1";

        var multiLine = block.Settings["multiLine"];
        multiLine.InputMode = SettingInputMode.Fixed;
        (multiLine.FixedSetting as BoolSetting)!.Value = true;

        var expected = $"string parsedOutput = string.Empty;{_nl}try {{{_nl}parsedOutput = MatchRegexGroups(data, data.SOURCE.AsString(), \"abc(.*)\", \"$1\", true, null, null, false);{_nl}data.LogVariableAssignment(nameof(parsedOutput));{_nl}data.MarkForCapture(nameof(parsedOutput));{_nl}}} catch (Exception safeException) {{{_nl}data.ERROR = safeException.PrettyPrint();{_nl}data.Logger.Log($\"[SAFE MODE] Exception caught and saved to data.ERROR: {{data.ERROR}}\", LogColors.Tomato); }}{_nl}";
        Assert.Equal(expected, block.ToCSharp([], new ConfigSettings()));
    }

    [Fact]
    public void ToSyntax_SafeRegexCapture_MatchesNormalizedLegacyOutput()
    {
        var block = CreateBlock();
        block.Safe = true;
        block.Mode = ParseMode.Regex;
        block.IsCapture = true;
        block.OutputVariable = "parsedOutput";

        var input = block.Settings["input"];
        input.InputMode = SettingInputMode.Variable;
        input.InputVariableName = "data.SOURCE";

        var pattern = block.Settings["pattern"];
        pattern.InputMode = SettingInputMode.Fixed;
        (pattern.FixedSetting as StringSetting)!.Value = "abc(.*)";

        var outputFormat = block.Settings["outputFormat"];
        outputFormat.InputMode = SettingInputMode.Fixed;
        (outputFormat.FixedSetting as StringSetting)!.Value = "$1";

        var multiLine = block.Settings["multiLine"];
        multiLine.InputMode = SettingInputMode.Fixed;
        (multiLine.FixedSetting as BoolSetting)!.Value = true;

        Assert.Equal(RenderLegacy(block, []), RenderSyntax(block, []));
    }

    [Fact]
    public void ToSyntax_ParityMatrix_MatchesLegacyAndVariableTracking()
    {
        AssertParity(CreateLrBlock(), []);
        AssertParity(CreateCssBlock(alreadyDeclared: true), ["parsedOutput"]);
        AssertParity(CreateXPathBlock(outputVariable: "globals.foundValue"), []);
        AssertParity(CreateJsonBlock(safe: true), []);
        AssertParity(CreateRegexBlock(safe: true, isCapture: true), []);
    }

    private static ParseBlockInstance CreateBlock()
        => new(new ParseBlockDescriptor());

    private static ParseBlockInstance CreateLrBlock()
    {
        var block = CreateBlock();
        block.Mode = ParseMode.LR;
        block.OutputVariable = "parsedOutput";

        block.Settings["input"].InputMode = SettingInputMode.Variable;
        block.Settings["input"].InputVariableName = "globals.source";
        (block.Settings["leftDelim"].FixedSetting as StringSetting)!.Value = "left";
        (block.Settings["rightDelim"].FixedSetting as StringSetting)!.Value = "right";
        (block.Settings["caseSensitive"].FixedSetting as BoolSetting)!.Value = true;
        (block.Settings["prefix"].FixedSetting as StringSetting)!.Value = "pre-";
        (block.Settings["suffix"].FixedSetting as StringSetting)!.Value = "-post";
        (block.Settings["urlEncodeOutput"].FixedSetting as BoolSetting)!.Value = true;

        return block;
    }

    private static ParseBlockInstance CreateCssBlock(bool alreadyDeclared = false)
    {
        var block = CreateBlock();
        block.Mode = ParseMode.CSS;
        block.OutputVariable = "parsedOutput";

        block.Settings["input"].InputMode = SettingInputMode.Variable;
        block.Settings["input"].InputVariableName = "data.SOURCE";
        (block.Settings["cssSelector"].FixedSetting as StringSetting)!.Value = ".item";
        (block.Settings["attributeName"].FixedSetting as StringSetting)!.Value = "href";

        if (alreadyDeclared)
        {
            block.Settings["prefix"].InputMode = SettingInputMode.Variable;
            block.Settings["prefix"].InputVariableName = "globals.prefix";
        }

        return block;
    }

    private static ParseBlockInstance CreateXPathBlock(string outputVariable)
    {
        var block = CreateBlock();
        block.Mode = ParseMode.XPath;
        block.OutputVariable = outputVariable;

        block.Settings["input"].InputMode = SettingInputMode.Variable;
        block.Settings["input"].InputVariableName = "input.html";
        (block.Settings["xPath"].FixedSetting as StringSetting)!.Value = "//a";
        (block.Settings["attributeName"].FixedSetting as StringSetting)!.Value = "title";

        return block;
    }

    private static ParseBlockInstance CreateJsonBlock(bool safe = false)
    {
        var block = CreateBlock();
        block.Safe = safe;
        block.Recursive = true;
        block.Mode = ParseMode.Json;
        block.OutputVariable = "parsedJson";

        block.Settings["input"].InputMode = SettingInputMode.Variable;
        block.Settings["input"].InputVariableName = "globals.payload";
        (block.Settings["jToken"].FixedSetting as StringSetting)!.Value = "$.items[*].name";
        (block.Settings["prefix"].FixedSetting as StringSetting)!.Value = "[";
        (block.Settings["suffix"].FixedSetting as StringSetting)!.Value = "]";

        return block;
    }

    private static ParseBlockInstance CreateRegexBlock(bool safe = false, bool isCapture = false)
    {
        var block = CreateBlock();
        block.Safe = safe;
        block.IsCapture = isCapture;
        block.Mode = ParseMode.Regex;
        block.OutputVariable = "parsedOutput";

        block.Settings["input"].InputMode = SettingInputMode.Variable;
        block.Settings["input"].InputVariableName = "data.SOURCE";
        (block.Settings["pattern"].FixedSetting as StringSetting)!.Value = "abc(.*)";
        (block.Settings["outputFormat"].FixedSetting as StringSetting)!.Value = "$1";
        (block.Settings["multiLine"].FixedSetting as BoolSetting)!.Value = true;

        return block;
    }

    private static void AssertParity(ParseBlockInstance block, List<string> definedVariables)
    {
        var legacyVariables = new List<string>(definedVariables);
        var syntaxVariables = new List<string>(definedVariables);
        var settings = new ConfigSettings();

        var legacy = StatementSyntaxParser.ParseStatements(block.ToCSharp(legacyVariables, settings)).ToSnippet();
        var syntax = block.ToSyntax(new BlockSyntaxGenerationContext(syntaxVariables, settings)).ToSnippet();

        Assert.Equal(legacy, syntax);
        Assert.Equal(legacyVariables, syntaxVariables);
    }

    private static string RenderLegacy(ParseBlockInstance block, List<string> definedVariables)
        => StatementSyntaxParser.ParseStatements(block.ToCSharp(definedVariables, new ConfigSettings())).ToSnippet();

    private static string RenderSyntax(ParseBlockInstance block, List<string> definedVariables)
        => block.ToSyntax(new BlockSyntaxGenerationContext(definedVariables, new ConfigSettings())).ToSnippet();
}
