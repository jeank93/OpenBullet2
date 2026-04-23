using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils.Mockup;
using SshMethods = RuriLib.Blocks.Requests.Ssh.Methods;
using Xunit;

namespace RuriLib.Tests.Blocks.Requests;

public class SshRequestBlocksTests
{
    [Fact]
    public void SshRunCommand_WithoutClient_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() =>
            SshMethods.SshRunCommand(data, "ls"));

        Assert.Equal("The SSH client is not initialized", ex.Message);
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
