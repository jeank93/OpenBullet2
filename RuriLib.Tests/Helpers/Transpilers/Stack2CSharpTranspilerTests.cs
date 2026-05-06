using RuriLib.Helpers.Blocks;
using RuriLib.Helpers.Transpilers;
using RuriLib.Models.Blocks;
using RuriLib.Models.Blocks.Custom;
using RuriLib.Models.Blocks.Custom.HttpRequest;
using RuriLib.Models.Blocks.Custom.Keycheck;
using RuriLib.Models.Blocks.Custom.Parse;
using RuriLib.Models.Blocks.Settings;
using RuriLib.Models.Configs;
using RuriLib.Models.Conditions.Comparisons;
using RuriLib.Models.Bots;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RuriLib.Tests.Helpers.Transpilers;

public class Stack2CSharpTranspilerTests
{
    [Fact]
    public void Transpile_MixedBlocks_RendersExpectedBlocksAndVariableFlow()
    {
        var settings = new ConfigSettings
        {
            GeneralSettings = { ContinueStatuses = ["SUCCESS", "NONE", "BAN"] }
        };

        var blocks = new List<BlockInstance>
        {
            CreateAutoBlock(),
            CreateParseBlock(),
            CreateKeycheckBlock(),
            CreateHttpRequestBlock()
        };

        var script = NormalizeGeneratedScript(Stack2CSharpTranspiler.Transpile(blocks, settings));

        Assert.Contains("// BLOCK: Substring", script);
        Assert.Contains("data.ExecutingBlock(\"Substring\");", script);
        Assert.Contains("string sharedValue = Substring(data, ObjectExtensions.DynamicAsString(globals.inputValue), 1, 4);", script);
        Assert.Contains("// BLOCK: Parse", script);
        Assert.Contains("sharedValue = MatchRegexGroups(data, sharedValue.AsString(), \"(.*)\", \"$1\", false, null, null, false);", script);
        Assert.Contains("// BLOCK: Keycheck", script);
        Assert.Contains("if (CheckCondition(data, sharedValue.AsString(), StrComparison.Contains, \"ok\"))", script);
        Assert.Contains("// BLOCK: HttpRequest", script);
        Assert.Contains("try", script);
        Assert.Contains("await HttpRequestBasicAuth(data, new BasicAuthHttpRequestOptions", script);
        Assert.Contains("Username = ObjectExtensions.DynamicAsString(globals.username)", script);
        Assert.DoesNotContain("string sharedValue = MatchRegexGroups", script);
    }

    [Fact]
    public void Transpile_StepByStepAndDisabledBlocks_SkipsDisabledAndInsertsStepper()
    {
        var settings = new ConfigSettings();
        var disabledBlock = CreateAutoBlock();
        disabledBlock.Disabled = true;

        var blocks = new List<BlockInstance>
        {
            disabledBlock,
            CreateAutoBlock(),
            CreateParseBlock()
        };

        var script = NormalizeGeneratedScript(Stack2CSharpTranspiler.Transpile(blocks, settings, stepByStep: true));

        Assert.Equal(1, script.Split("// BLOCK:").Count(segment => segment.Contains("Substring")));
        Assert.DoesNotContain("data.ExecutingBlock(\"\");", script);
        Assert.Contains("// BLOCK: Parse", script);
        Assert.Equal(1, script.Split("await data.Stepper.WaitForStepAsync(data.CancellationToken);").Length - 1);
    }

    private static AutoBlockInstance CreateAutoBlock()
    {
        var block = BlockFactory.GetBlock<AutoBlockInstance>("Substring");
        block.Label = "Substring";
        block.OutputVariable = "sharedValue";
        block.Settings["input"].InputMode = SettingInputMode.Variable;
        block.Settings["input"].InputVariableName = "globals.inputValue";
        (block.Settings["index"].FixedSetting as IntSetting)!.Value = 1;
        (block.Settings["length"].FixedSetting as IntSetting)!.Value = 4;

        return block;
    }

    private static ParseBlockInstance CreateParseBlock()
    {
        var block = new ParseBlockInstance(new ParseBlockDescriptor())
        {
            Label = "Parse",
            OutputVariable = "sharedValue",
            Mode = ParseMode.Regex
        };

        block.Settings["input"].InputMode = SettingInputMode.Variable;
        block.Settings["input"].InputVariableName = "sharedValue";
        (block.Settings["pattern"].FixedSetting as StringSetting)!.Value = "(.*)";
        (block.Settings["outputFormat"].FixedSetting as StringSetting)!.Value = "$1";
        (block.Settings["multiLine"].FixedSetting as BoolSetting)!.Value = false;

        return block;
    }

    private static KeycheckBlockInstance CreateKeycheckBlock()
    {
        var block = new KeycheckBlockInstance(new KeycheckBlockDescriptor())
        {
            Label = "Keycheck"
        };

        block.Settings["banIfNoMatch"].InputMode = SettingInputMode.Variable;
        block.Settings["banIfNoMatch"].InputVariableName = "globals.shouldBan";
        block.Keychains =
        [
            new Keychain
            {
                ResultStatus = "SUCCESS",
                Mode = KeychainMode.AND,
                Keys =
                [
                    new StringKey
                    {
                        Left = BlockSettingFactory.CreateStringSetting("", "sharedValue", SettingInputMode.Variable),
                        Comparison = StrComparison.Contains,
                        Right = BlockSettingFactory.CreateStringSetting("", "ok", SettingInputMode.Fixed)
                    }
                ]
            }
        ];

        return block;
    }

    private static HttpRequestBlockInstance CreateHttpRequestBlock()
    {
        var block = BlockFactory.GetBlock<HttpRequestBlockInstance>("HttpRequest");
        block.Label = "HttpRequest";
        block.Safe = true;
        block.RequestParams = new BasicAuthRequestParams
        {
            Username = BlockSettingFactory.CreateStringSetting("username", "globals.username", SettingInputMode.Variable),
            Password = BlockSettingFactory.CreateStringSetting("password", "globals.password", SettingInputMode.Variable)
        };

        (block.Settings["method"].FixedSetting as EnumSetting)!.Value = "GET";
        (block.Settings["url"].FixedSetting as StringSetting)!.Value = "https://example.com";

        return block;
    }

    private static string NormalizeGeneratedScript(string script)
        => string.Join("\n", script
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => !string.IsNullOrWhiteSpace(line)));
}
