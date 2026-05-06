using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RuriLib.Helpers.Blocks;
using RuriLib.Helpers.CSharp;
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
using System.IO;
using System.Linq;
using Xunit;

namespace RuriLib.Tests.Helpers.Transpilers;

public class Stack2CSharpTranspilerTests
{
    [Fact]
    public void Transpile_MixedBlocks_MatchesLegacyRendering()
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

        Assert.Equal(
            NormalizeGeneratedScript(RenderLegacyTranspilerOutput(blocks, settings)),
            NormalizeGeneratedScript(Stack2CSharpTranspiler.Transpile(blocks, settings)));
    }

    [Fact]
    public void Transpile_StepByStepAndDisabledBlocks_MatchesLegacyRendering()
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

        Assert.Equal(
            NormalizeGeneratedScript(RenderLegacyTranspilerOutput(blocks, settings, stepByStep: true)),
            NormalizeGeneratedScript(Stack2CSharpTranspiler.Transpile(blocks, settings, stepByStep: true)));
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

    private static string RenderLegacyTranspilerOutput(List<BlockInstance> blocks, ConfigSettings settings,
        bool stepByStep = false)
    {
        var declaredVariables = typeof(BotData).GetProperties()
            .Select(p => $"data.{p.Name}")
            .ToList();

        using var writer = new StringWriter();
        var validBlocks = blocks.Where(b => !b.Disabled).ToList();

        foreach (var block in validBlocks)
        {
            writer.WriteLine($"// BLOCK: {block.Label}");
            writer.WriteLine($"data.ExecutingBlock({CSharpWriter.SerializeString(block.Label)});");

            var snippet = block.ToCSharp(declaredVariables, settings);
            var tree = CSharpSyntaxTree.ParseText(snippet);
            writer.WriteLine(tree.GetRoot().NormalizeWhitespace().ToFullString());
            writer.WriteLine();

            if (stepByStep && block != validBlocks.Last())
            {
                writer.WriteLine("await data.Stepper.WaitForStepAsync(data.CancellationToken);");
            }
        }

        return writer.ToString();
    }

    private static string NormalizeGeneratedScript(string script)
        => string.Join("\n", script
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => !string.IsNullOrWhiteSpace(line)));
}
