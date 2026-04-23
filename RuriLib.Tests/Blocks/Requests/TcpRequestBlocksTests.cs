using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils.Mockup;
using System.Threading.Tasks;
using TcpMethods = RuriLib.Blocks.Requests.Tcp.Methods;
using Xunit;

namespace RuriLib.Tests.Blocks.Requests;

public class TcpRequestBlocksTests
{
    [Fact]
    public async Task TcpRead_WithoutConnection_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            TcpMethods.TcpRead(data));

        Assert.Equal("You have to create a connection first!", ex.Message);
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
