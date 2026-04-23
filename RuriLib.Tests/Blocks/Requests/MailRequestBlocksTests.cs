using MailKit;
using FtpMethods = RuriLib.Blocks.Requests.Ftp.Methods;
using ImapMethods = RuriLib.Blocks.Requests.Imap.Methods;
using Pop3Methods = RuriLib.Blocks.Requests.Pop3.Methods;
using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using SmtpMethods = RuriLib.Blocks.Requests.Smtp.Methods;
using SshMethods = RuriLib.Blocks.Requests.Ssh.Methods;
using TcpMethods = RuriLib.Blocks.Requests.Tcp.Methods;
using RuriLib.Tests.Utils.Mockup;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WsMethods = RuriLib.Blocks.Requests.WebSocket.Methods;
using Xunit;

namespace RuriLib.Tests.Blocks.Requests;

public class MailRequestBlocksTests
{
    [Fact]
    public void ImapGetLog_WithoutLogger_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() => ImapMethods.ImapGetLog(data));

        Assert.Equal("The IMAP protocol logger is not initialized", ex.Message);
    }

    [Fact]
    public void Pop3GetLog_WithoutLogger_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() => Pop3Methods.Pop3GetLog(data));

        Assert.Equal("The POP3 protocol logger is not initialized", ex.Message);
    }

    [Fact]
    public void SmtpGetLog_WithoutLogger_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() => SmtpMethods.SmtpGetLog(data));

        Assert.Equal("The SMTP protocol logger is not initialized", ex.Message);
    }

    [Fact]
    public void MailGetLog_WithLogger_ReturnsContent()
    {
        var data = NewBotData();
        data.SetObject("imapLogger", CreateLogger("imap-log"));
        data.SetObject("pop3Logger", CreateLogger("pop3-log"));
        data.SetObject("smtpLogger", CreateLogger("smtp-log"));

        Assert.Equal("imap-log", ImapMethods.ImapGetLog(data));
        Assert.Equal("pop3-log", Pop3Methods.Pop3GetLog(data));
        Assert.Equal("smtp-log", SmtpMethods.SmtpGetLog(data));
    }

    [Fact]
    public async Task ImapOpenFolder_WithoutFolderCache_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            ImapMethods.ImapOpenFolder(data, "Inbox"));

        Assert.Equal("Get the list of folders first!", ex.Message);
    }

    [Fact]
    public async Task Pop3DeleteMail_WithoutClient_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            Pop3Methods.Pop3DeleteMail(data, 0));

        Assert.Equal("Connect the POP3 client first!", ex.Message);
    }

    [Fact]
    public async Task SmtpSendMail_WithoutClient_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            SmtpMethods.SmtpSendMail(
                data,
                "Sender",
                "sender@example.com",
                "Recipient",
                "recipient@example.com",
                "Subject",
                "Text body",
                "<p>Html body</p>"));

        Assert.Equal("Connect the SMTP client first!", ex.Message);
    }

    [Fact]
    public void SshRunCommand_WithoutClient_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() =>
            SshMethods.SshRunCommand(data, "ls"));

        Assert.Equal("The SSH client is not initialized", ex.Message);
    }

    [Fact]
    public async Task TcpRead_WithoutConnection_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            TcpMethods.TcpRead(data));

        Assert.Equal("You have to create a connection first!", ex.Message);
    }

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
    public void FtpGetLog_WithoutLogger_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() =>
            FtpMethods.FtpGetLog(data));

        Assert.Equal("No log available. Make sure to connect to a server first!", ex.Message);
    }

    [Fact]
    public async Task FtpListItems_WithoutClient_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            FtpMethods.FtpListItems(data));

        Assert.Equal("Connect to a server first!", ex.Message);
    }

    private static ProtocolLogger CreateLogger(string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content))
        {
            Position = 0
        };
        return new ProtocolLogger(stream);
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
