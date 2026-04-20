using RuriLib.Blocks.Functions.String;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Providers.RandomNumbers;
using RuriLib.Tests.Utils.Mockup;
using System;
using System.Collections.Generic;
using Xunit;

namespace RuriLib.Tests.Blocks.Functions;

public class StringFunctionsTests
{
    [Fact]
    public void Translate_ReplaceOne_PrefersLongestMatch()
    {
        var data = NewBotData();
        var translations = new Dictionary<string, string>
        {
            ["ab"] = "X",
            ["a"] = "Y"
        };

        var translated = Methods.Translate(data, "ab", translations, replaceOne: true);

        Assert.Equal("X", translated);
    }

    [Fact]
    public void UrlEncode_LongInput_RoundTrips()
    {
        var data = NewBotData();
        var input = new string('a', 3000) + " ?=+" + new string('b', 3000);

        var encoded = Methods.UrlEncode(data, input);
        var decoded = Methods.UrlDecode(data, encoded);

        Assert.Equal(input, decoded);
    }

    [Fact]
    public void RandomString_CustomCharset_UsesProvidedCharacters()
    {
        var data = NewBotData();

        var result = Methods.RandomString(data, "?c?c?c", "Z");

        Assert.Equal("ZZZ", result);
    }

    [Fact]
    public void Split_Separator_ReturnsItems()
    {
        var data = NewBotData();

        var split = Methods.Split(data, "a:b:c", ":");

        Assert.Equal(["a", "b", "c"], split);
    }

    private static BotData NewBotData()
        => new(
            new global::RuriLib.Models.Bots.Providers(null!)
            {
                RNG = new DeterministicRngProvider(),
                ProxySettings = new MockedProxySettingsProvider(),
                Security = new MockedSecurityProvider()
            },
            new ConfigSettings(),
            new BotLogger(),
            new DataLine("hello", new WordlistType()));

    private sealed class DeterministicRngProvider : IRNGProvider
    {
        public Random GetNew() => new(12345);
    }
}
