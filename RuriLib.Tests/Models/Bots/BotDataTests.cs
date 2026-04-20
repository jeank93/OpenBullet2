using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils.Mockup;
using System;
using Xunit;

namespace RuriLib.Tests.Models.Bots;

public class BotDataTests
{
    [Fact]
    public void SetObject_DisposesExistingValue_WhenReplacing()
    {
        var botData = NewBotData();
        var first = new DisposableStub();
        var second = new DisposableStub();

        botData.SetObject("resource", first);
        botData.SetObject("resource", second);

        Assert.True(first.Disposed);
        Assert.False(second.Disposed);
    }

    [Fact]
    public void ResetState_DisposesRetryScopedObjectsAndKeepsWhitelistedOnes()
    {
        var botData = NewBotData();
        var disposable = new DisposableStub();
        var whitelisted = new DisposableStub();

        botData.STATUS = "GOOD";
        botData.SOURCE = "source";
        botData.ERROR = "error";
        botData.RESPONSECODE = 200;
        botData.COOKIES["a"] = "b";
        botData.HEADERS["c"] = "d";
        botData.MarkForCapture("captured");
        botData.SetObject("custom", disposable);
        botData.SetObject("httpClient", whitelisted);

        botData.ResetState();

        Assert.True(disposable.Disposed);
        Assert.False(whitelisted.Disposed);
        Assert.Equal("NONE", botData.STATUS);
        Assert.Equal(string.Empty, botData.SOURCE);
        Assert.Equal(string.Empty, botData.ERROR);
        Assert.Equal(0, botData.RESPONSECODE);
        Assert.Empty(botData.COOKIES);
        Assert.Empty(botData.HEADERS);
        Assert.Empty(botData.MarkedForCapture);
    }

    [Fact]
    public void TryGetObject_EmptyName_Throws()
    {
        var botData = NewBotData();

        Assert.Throws<ArgumentException>(() => botData.TryGetObject<object>(string.Empty));
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

    private sealed class DisposableStub : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
}
