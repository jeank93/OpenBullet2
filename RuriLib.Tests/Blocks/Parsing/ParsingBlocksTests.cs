using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils.Mockup;
using Xunit;

namespace RuriLib.Tests.Blocks.Parsing;

public class ParsingBlocksTests
{
    [Fact]
    public void ParseBetweenStringsRecursive_UrlEncodeOutput_EncodesValues()
    {
        var data = NewBotData();

        var parsed = global::RuriLib.Blocks.Parsing.Methods.ParseBetweenStringsRecursive(
            data,
            "a[href='hello world'] b[href='two/three']",
            "[href='",
            "']",
            urlEncodeOutput: true);

        Assert.Equal(["hello%20world", "two%2Fthree"], parsed);
    }

    [Fact]
    public void QueryJsonTokenRecursive_UrlEncodeOutput_EncodesValues()
    {
        var data = NewBotData();

        var parsed = global::RuriLib.Blocks.Parsing.Methods.QueryJsonTokenRecursive(
            data,
            "{\"value\":\"hello world\"}",
            "value",
            urlEncodeOutput: true);

        Assert.Equal(["hello%20world"], parsed);
    }

    [Fact]
    public void MatchRegexGroupsRecursive_UrlEncodeOutput_EncodesValues()
    {
        var data = NewBotData();

        var parsed = global::RuriLib.Blocks.Parsing.Methods.MatchRegexGroupsRecursive(
            data,
            "url=hello world",
            "url=(.*)",
            "[1]",
            multiLine: false,
            urlEncodeOutput: true);

        Assert.Equal(["hello%20world"], parsed);
    }

    private static BotData NewBotData()
        => new(
            new global::RuriLib.Models.Bots.Providers(null!)
            {
                ProxySettings = new MockedProxySettingsProvider(),
                Security = new MockedSecurityProvider()
            },
            new ConfigSettings(),
            new BotLogger(),
            new DataLine("hello", new WordlistType()));
}
