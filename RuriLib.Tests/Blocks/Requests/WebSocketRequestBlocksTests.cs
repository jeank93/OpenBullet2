using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils.Mockup;
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
