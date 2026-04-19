using System;
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

    private static ParseBlockInstance CreateBlock()
        => new(new ParseBlockDescriptor());
}
