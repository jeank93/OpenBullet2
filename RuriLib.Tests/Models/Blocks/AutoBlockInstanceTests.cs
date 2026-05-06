using RuriLib.Helpers.Blocks;
using RuriLib.Helpers.CSharp;
using RuriLib.Models.Blocks;
using RuriLib.Models.Blocks.Settings;
using RuriLib.Models.Blocks.Settings.Interpolated;
using RuriLib.Models.Configs;
using System;
using System.Collections.Generic;
using Xunit;

namespace RuriLib.Tests.Models.Blocks;

public class AutoBlockInstanceTests
{
    private readonly string _nl = Environment.NewLine;

    [Fact]
    public void ToLC_ParseLRBlock_OutputScript()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("Substring");
        block.OutputVariable = "myOutput";
        block.IsCapture = false;
        block.Disabled = true;
        block.Label = "My Label";
        var input = block.Settings["input"];
        var index = block.Settings["index"];
        var length = block.Settings["length"];

        input.InputMode = SettingInputMode.Variable;
        input.InputVariableName = "myInput";

        index.InputMode = SettingInputMode.Fixed;
        (index.FixedSetting as IntSetting)!.Value = 3;

        length.InputMode = SettingInputMode.Fixed;

        var expected = $"DISABLED{_nl}LABEL:My Label{_nl}  input = @myInput{_nl}  index = 3{_nl}  => VAR @myOutput{_nl}";
        Assert.Equal(expected, block.ToLC());
    }

    [Fact]
    public void FromLC_ParseLRBlock_BuildBlock()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("Substring");
        var script = $"DISABLED{_nl}LABEL:My Label{_nl}  input = @myInput{_nl}  index = 3{_nl}  => CAP @myOutput{_nl}";
        var lineNumber = 0;
        block.FromLC(ref script, ref lineNumber);

        Assert.True(block.Disabled);
        Assert.Equal("My Label", block.Label);
        Assert.Equal("myOutput", block.OutputVariable);
        Assert.True(block.IsCapture);

        var input = block.Settings["input"];
        var index = block.Settings["index"];

        Assert.Equal(SettingInputMode.Variable, input.InputMode);
        Assert.Equal("myInput", input.InputVariableName);
        Assert.Equal(SettingInputMode.Fixed, index.InputMode);
        Assert.Equal(3, (index.FixedSetting as IntSetting)!.Value);
    }

    [Fact]
    public void ToCSharp_SyncReturnValue_OutputScript()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("Substring");
        block.OutputVariable = "myOutput";
        var input = block.Settings["input"];
        var index = block.Settings["index"];
        var length = block.Settings["length"];

        input.InputMode = SettingInputMode.Variable;
        input.InputVariableName = "myInput";

        index.InputMode = SettingInputMode.Fixed;
        (index.FixedSetting as IntSetting)!.Value = 3;

        length.InputMode = SettingInputMode.Fixed;
        (length.FixedSetting as IntSetting)!.Value = 5;

        var expected = $"string myOutput = Substring(data, myInput.AsString(), 3, 5);{_nl}data.LogVariableAssignment(nameof(myOutput));{_nl}";
        Assert.Equal(expected, block.ToCSharp([], new ConfigSettings()));
    }

    [Fact]
    public void ToCSharp_SyncReturnValueCapture_OutputScript()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("Substring");
        block.OutputVariable = "myOutput";
        block.IsCapture = true;
        var input = block.Settings["input"];
        var index = block.Settings["index"];
        var length = block.Settings["length"];

        input.InputMode = SettingInputMode.Variable;
        input.InputVariableName = "myInput";

        index.InputMode = SettingInputMode.Fixed;
        (index.FixedSetting as IntSetting)!.Value = 3;

        length.InputMode = SettingInputMode.Fixed;
        (length.FixedSetting as IntSetting)!.Value = 5;

        var expected = $"string myOutput = Substring(data, myInput.AsString(), 3, 5);{_nl}data.LogVariableAssignment(nameof(myOutput));{_nl}data.MarkForCapture(nameof(myOutput));{_nl}";
        Assert.Equal(expected, block.ToCSharp([], new ConfigSettings()));
    }

    [Fact]
    public void ToCSharp_AsyncNoReturnValue_OutputScript()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("TcpConnect");
        var url = block.Settings["host"];
        var port = block.Settings["port"];
        var ssl = block.Settings["useSSL"];
        var timeout = block.Settings["timeoutMilliseconds"];

        (url.FixedSetting as StringSetting)!.Value = "example.com";
        (port.FixedSetting as IntSetting)!.Value = 80;
        (ssl.FixedSetting as BoolSetting)!.Value = false;
        (timeout.FixedSetting as IntSetting)!.Value = 1000;

        var expected = $"await TcpConnect(data, \"example.com\", 80, false, 1000).ConfigureAwait(false);{_nl}";
        Assert.Equal(expected, block.ToCSharp([], new ConfigSettings()));
    }

    [Fact]
    public void ToCSharp_BlockIdOverride_UsesAsyncMethodName()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("FileExists");
        block.OutputVariable = "exists";
        var path = block.Settings["path"];

        path.InputMode = SettingInputMode.Fixed;
        (path.FixedSetting as StringSetting)!.Value = "test.txt";

        var expected = $"bool exists = await FileExistsAsync(data, \"test.txt\").ConfigureAwait(false);{_nl}data.LogVariableAssignment(nameof(exists));{_nl}";
        Assert.Equal(expected, block.ToCSharp([], new ConfigSettings()));
    }

    [Fact]
    public void ToCSharp_SyncReturnValueAlreadyDeclared_OutputScript()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("Substring");
        block.OutputVariable = "myOutput";
        var input = block.Settings["input"];
        var index = block.Settings["index"];
        var length = block.Settings["length"];

        input.InputMode = SettingInputMode.Interpolated;
        input.InterpolatedSetting = new InterpolatedStringSetting { Value = "my <interp> string" };

        index.InputMode = SettingInputMode.Fixed;
        (index.FixedSetting as IntSetting)!.Value = 3;

        length.InputMode = SettingInputMode.Variable;
        length.InputVariableName = "myLength";

        var expected = $"myOutput = Substring(data, $\"my {{interp}} string\", 3, myLength.AsInt());{_nl}data.LogVariableAssignment(nameof(myOutput));{_nl}";
        Assert.Equal(expected, block.ToCSharp(["myOutput"], new ConfigSettings()));
    }

    [Fact]
    public void ToCSharp_SyncReturnValueEscapedAngleBrackets_OutputScript()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("Substring");
        block.OutputVariable = "myOutput";
        var input = block.Settings["input"];
        var index = block.Settings["index"];
        var length = block.Settings["length"];

        input.InputMode = SettingInputMode.Interpolated;
        input.InterpolatedSetting = new InterpolatedStringSetting { Value = "hello <<<name>>> and <<friend>>" };

        index.InputMode = SettingInputMode.Fixed;
        (index.FixedSetting as IntSetting)!.Value = 3;

        length.InputMode = SettingInputMode.Variable;
        length.InputVariableName = "myLength";

        var expected = $"myOutput = Substring(data, $\"hello <{{name}}> and <friend>\", 3, myLength.AsInt());{_nl}data.LogVariableAssignment(nameof(myOutput));{_nl}";
        Assert.Equal(expected, block.ToCSharp(["myOutput"], new ConfigSettings()));
    }

    [Fact]
    public void ToSyntax_SyncReturnValue_MatchesToCSharp()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("Substring");
        block.OutputVariable = "myOutput";
        var input = block.Settings["input"];
        var index = block.Settings["index"];
        var length = block.Settings["length"];

        input.InputMode = SettingInputMode.Variable;
        input.InputVariableName = "myInput";

        index.InputMode = SettingInputMode.Fixed;
        (index.FixedSetting as IntSetting)!.Value = 3;

        length.InputMode = SettingInputMode.Fixed;
        (length.FixedSetting as IntSetting)!.Value = 5;

        var expected = block.ToCSharp([], new ConfigSettings());
        Assert.Equal(expected, RenderSyntax(block, []));
    }

    [Fact]
    public void ToSyntax_SyncReturnValueCapture_MatchesToCSharp()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("Substring");
        block.OutputVariable = "myOutput";
        block.IsCapture = true;
        var input = block.Settings["input"];
        var index = block.Settings["index"];
        var length = block.Settings["length"];

        input.InputMode = SettingInputMode.Variable;
        input.InputVariableName = "myInput";

        index.InputMode = SettingInputMode.Fixed;
        (index.FixedSetting as IntSetting)!.Value = 3;

        length.InputMode = SettingInputMode.Fixed;
        (length.FixedSetting as IntSetting)!.Value = 5;

        var expected = block.ToCSharp([], new ConfigSettings());
        Assert.Equal(expected, RenderSyntax(block, []));
    }

    [Fact]
    public void ToSyntax_AsyncNoReturnValue_MatchesToCSharp()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("TcpConnect");
        var url = block.Settings["host"];
        var port = block.Settings["port"];
        var ssl = block.Settings["useSSL"];
        var timeout = block.Settings["timeoutMilliseconds"];

        (url.FixedSetting as StringSetting)!.Value = "example.com";
        (port.FixedSetting as IntSetting)!.Value = 80;
        (ssl.FixedSetting as BoolSetting)!.Value = false;
        (timeout.FixedSetting as IntSetting)!.Value = 1000;

        var expected = block.ToCSharp([], new ConfigSettings());
        Assert.Equal(expected, RenderSyntax(block, []));
    }

    [Fact]
    public void ToSyntax_ParityMatrix_MatchesLegacyAndVariableTracking()
    {
        AssertParity(CreateSubstringBlock(), []);
        AssertParity(CreateSubstringBlock(isCapture: true), []);
        AssertParity(CreateSubstringBlock(safe: true), []);
        AssertParity(CreateSubstringBlock(outputVariable: "globals.sharedSlice", useGlobalsInput: true), []);
        AssertParity(CreateSubstringBlock(alreadyDeclared: true, interpolatedInput: "hello <name>"), ["myOutput"]);
        AssertParity(CreateFileExistsBlock(), []);
        AssertParity(CreateFileExistsBlock(safe: true, outputVariable: "globals.fileExists"), []);
        AssertParity(CreateTcpConnectBlock(), []);
    }

    private static AutoBlockInstance CreateSubstringBlock(
        bool safe = false,
        bool isCapture = false,
        bool alreadyDeclared = false,
        string outputVariable = "myOutput",
        bool useGlobalsInput = false,
        string? interpolatedInput = null)
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("Substring");
        block.Safe = safe;
        block.IsCapture = isCapture;
        block.OutputVariable = outputVariable;

        var input = block.Settings["input"];
        var index = block.Settings["index"];
        var length = block.Settings["length"];

        if (interpolatedInput is not null)
        {
            input.InputMode = SettingInputMode.Interpolated;
            input.InterpolatedSetting = new InterpolatedStringSetting { Value = interpolatedInput };
        }
        else
        {
            input.InputMode = SettingInputMode.Variable;
            input.InputVariableName = useGlobalsInput ? "globals.inputValue" : "myInput";
        }

        index.InputMode = SettingInputMode.Fixed;
        (index.FixedSetting as IntSetting)!.Value = 3;

        if (alreadyDeclared)
        {
            length.InputMode = SettingInputMode.Variable;
            length.InputVariableName = "input.length";
        }
        else
        {
            length.InputMode = SettingInputMode.Fixed;
            (length.FixedSetting as IntSetting)!.Value = 5;
        }

        return block;
    }

    private static AutoBlockInstance CreateFileExistsBlock(bool safe = false, string outputVariable = "exists")
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("FileExists");
        block.Safe = safe;
        block.OutputVariable = outputVariable;

        var path = block.Settings["path"];
        path.InputMode = SettingInputMode.Variable;
        path.InputVariableName = "globals.filePath";

        return block;
    }

    private static AutoBlockInstance CreateTcpConnectBlock()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("TcpConnect");
        var url = block.Settings["host"];
        var port = block.Settings["port"];
        var ssl = block.Settings["useSSL"];
        var timeout = block.Settings["timeoutMilliseconds"];

        url.InputMode = SettingInputMode.Variable;
        url.InputVariableName = "globals.host";
        (port.FixedSetting as IntSetting)!.Value = 80;
        (ssl.FixedSetting as BoolSetting)!.Value = false;
        timeout.InputMode = SettingInputMode.Variable;
        timeout.InputVariableName = "input.timeout";

        return block;
    }

    private static void AssertParity(BlockInstance block, List<string> definedVariables)
    {
        var legacyVariables = new List<string>(definedVariables);
        var syntaxVariables = new List<string>(definedVariables);
        var settings = new ConfigSettings();

        var legacy = StatementSyntaxParser.ParseStatements(block.ToCSharp(legacyVariables, settings)).ToSnippet();
        var syntax = block.ToSyntax(new BlockSyntaxGenerationContext(syntaxVariables, settings)).ToSnippet();

        Assert.Equal(legacy, syntax);
        Assert.Equal(legacyVariables, syntaxVariables);
    }

    private static string RenderSyntax(BlockInstance block, List<string> definedVariables)
        => block.ToSyntax(new BlockSyntaxGenerationContext(definedVariables, new ConfigSettings())).ToSnippet();
}
