using RuriLib.Exceptions;
using RuriLib.Functions.Conversion;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Models.Proxies;
using RuriLib.Tests.Utils;
using RuriLib.Tests.Utils.Mockup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WsMethods = RuriLib.Blocks.Requests.WebSocket.Methods;
using Xunit;

namespace RuriLib.Tests.Blocks.Requests;

public class WebSocketRequestBlocksTests
{
    [Fact]
    public async Task WsRead_WithoutConnection_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            WsMethods.WsRead(data));

        Assert.Equal("You must open a websocket connection first", ex.Message);
    }

    [Fact]
    public void WsSend_WithoutConnection_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() =>
            WsMethods.WsSend(data, "ping"));

        Assert.Equal("You must open a websocket connection first", ex.Message);
    }

    [Fact]
    public void WsDisconnect_WithoutConnection_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() =>
            WsMethods.WsDisconnect(data));

        Assert.Equal("You must open a websocket connection first", ex.Message);
    }

    [Fact]
    public async Task WsConnect_SendRead_AndDisconnect_Verify()
    {
        var url = await TestWebSocketServer.BuildEchoUrl();
        var data = NewBotData();

        try
        {
            await WsMethods.WsConnect(
                data,
                url,
                customHeaders: new Dictionary<string, string> { ["X-OB2-Test"] = "websocket" });

            Assert.NotNull(data.TryGetObject<object>("webSocket"));

            WsMethods.WsSend(data, "ping");

            var messages = await WsMethods.WsRead(data, timeoutMilliseconds: 5000);
            var unreadMessages = data.TryGetObject<List<string>>("wsMessages");

            Assert.Single(messages);
            Assert.Equal("ping", messages[0]);
            Assert.NotNull(unreadMessages);
            Assert.Empty(unreadMessages!);

            WsMethods.WsDisconnect(data);

            Assert.Null(data.TryGetObject<object>("webSocket"));
        }
        finally
        {
            DisposeClient(data);
        }
    }

    [Fact]
    public async Task WsConnect_SendRaw_EncodesBinaryMessagesAsBase64()
    {
        var url = await TestWebSocketServer.BuildEchoUrl();
        var data = NewBotData();
        var payload = new byte[] { 0, 1, 2, 127, 255 };

        try
        {
            await WsMethods.WsConnect(data, url);

            WsMethods.WsSendRaw(data, payload);

            var messages = await WsMethods.WsRead(data, timeoutMilliseconds: 5000);

            Assert.Single(messages);
            Assert.Equal(Base64Converter.ToBase64String(payload), messages[0]);
        }
        finally
        {
            DisposeClient(data);
        }
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

    private static void DisposeClient(BotData data)
    {
        if (data.TryGetObject<object>("webSocket") is null)
        {
            return;
        }

        try
        {
            WsMethods.WsDisconnect(data);
        }
        catch
        {
        }
    }
}
