using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Providers.RandomNumbers;
using RuriLib.Providers.UserAgents;
using RuriLib.Tests.Utils.Mockup;
using System;
using Xunit;

namespace RuriLib.Tests.Blocks.Functions;

public class GeneralAndTimeFunctionsTests
{
    [Fact]
    public void RandomUserAgent_ReturnsGeneratedValue()
    {
        var data = NewBotData(new StubUaProvider("ua-mobile"));

        var result = global::RuriLib.Blocks.Functions.Methods.RandomUserAgent(data, UAPlatform.Mobile);

        Assert.Equal("ua-mobile", result);
    }

    [Fact]
    public void RandomUserAgent_WhenProviderThrows_ReturnsFallback()
    {
        var data = NewBotData(new ThrowingUaProvider());

        var result = global::RuriLib.Blocks.Functions.Methods.RandomUserAgent(data);

        Assert.Equal("NO_RANDOM_UA_FOUND", result);
    }

    [Fact]
    public void DateToUnixTime_ParsesAsLocalTime()
    {
        var data = NewBotData(new StubUaProvider("ua"));
        const string datetime = "2020-04-18:00-00-00";
        const string format = "yyyy-MM-dd:HH-mm-ss";
        var expected = (int)DateTime.ParseExact(datetime, format, null).ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        var parsed = global::RuriLib.Blocks.Functions.Time.Methods.DateToUnixTime(data, datetime, format);

        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void UnixTimeToIso8601_FormatsExpectedValue()
    {
        var data = NewBotData(new StubUaProvider("ua"));

        var iso = global::RuriLib.Blocks.Functions.Time.Methods.UnixTimeToISO8601(data, 1587168000);

        Assert.Equal("2020-04-18T00:00:00.000Z", iso);
    }

    private static BotData NewBotData(IRandomUAProvider randomUaProvider)
        => new(
            new global::RuriLib.Models.Bots.Providers(null!)
            {
                RNG = new DeterministicRngProvider(),
                RandomUA = randomUaProvider,
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

    private sealed class StubUaProvider(string userAgent) : IRandomUAProvider
    {
        public int Total => 1;

        public string Generate() => userAgent;

        public string Generate(UAPlatform platform) => userAgent;
    }

    private sealed class ThrowingUaProvider : IRandomUAProvider
    {
        public int Total => 0;

        public string Generate() => throw new InvalidOperationException("No user agents");

        public string Generate(UAPlatform platform) => throw new InvalidOperationException("No user agents");
    }
}
