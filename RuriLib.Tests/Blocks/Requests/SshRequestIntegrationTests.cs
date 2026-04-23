using Renci.SshNet;
using Renci.SshNet.Common;
using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils;
using RuriLib.Tests.Utils.Mockup;
using SshMethods = RuriLib.Blocks.Requests.Ssh.Methods;
using System;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Blocks.Requests;

[Collection(nameof(SshServerCollection))]
public class SshRequestIntegrationTests
{
    [Fact]
    public async Task SshAuthenticateWithPassword_AndRunCommand_Verify()
    {
        var connection = await TestSshServer.GetConnectionInfo();
        var data = NewBotData();

        try
        {
            SshMethods.SshAuthenticateWithPassword(
                data,
                connection.Host,
                connection.Port,
                connection.Username,
                connection.Password,
                20000,
                5000,
                3);

            var result = SshMethods.SshRunCommand(data, "printf ssh-ok");
            var client = data.TryGetObject<SshClient>("sshClient");

            Assert.NotNull(client);
            Assert.True(client!.IsConnected);
            Assert.Equal("ssh-ok", result);
        }
        finally
        {
            DisposeClient(data);
        }
    }

    [Fact]
    public async Task SshAuthenticateWithPassword_WrongPassword_Throws()
    {
        var connection = await TestSshServer.GetConnectionInfo();
        var data = NewBotData();

        var ex = Assert.Throws<SshAuthenticationException>(() =>
            SshMethods.SshAuthenticateWithPassword(
                data,
                connection.Host,
                connection.Port,
                connection.Username,
                "wrong-password",
                10000,
                5000,
                1));

        Assert.Contains("Permission denied", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SshAuthenticateWithNone_Throws()
    {
        var connection = await TestSshServer.GetConnectionInfo();
        var data = NewBotData();

        var ex = Assert.Throws<SshAuthenticationException>(() =>
            SshMethods.SshAuthenticateWithNone(
                data,
                connection.Host,
                connection.Port,
                connection.Username,
                10000,
                5000,
                1));

        Assert.Contains("authentication", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SshAuthenticateWithPK_MissingKeyFile_Throws()
    {
        var data = NewBotData();

        Assert.ThrowsAny<Exception>(() =>
            SshMethods.SshAuthenticateWithPK(
                data,
                "127.0.0.1",
                22,
                "ob2-user",
                "missing.key",
                "",
                1000,
                1000,
                1));
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
            new DataLine("ssh-test", new WordlistType()));

    private static void DisposeClient(BotData data)
    {
        if (data.TryGetObject<SshClient>("sshClient") is { } client)
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }

            client.Dispose();
            data.SetObject("sshClient", null, disposeExisting: false);
        }
    }
}

[CollectionDefinition(nameof(SshServerCollection), DisableParallelization = true)]
public class SshServerCollection;
