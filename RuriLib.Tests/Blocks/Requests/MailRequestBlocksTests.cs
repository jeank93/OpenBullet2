using MailKit;
using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils.Mockup;
using System.IO;
using System.Text;
using Xunit;

namespace RuriLib.Tests.Blocks.Requests;

public class MailRequestBlocksTests
{
    [Fact]
    public void ImapGetLog_WithoutLogger_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() => global::RuriLib.Blocks.Requests.Imap.Methods.ImapGetLog(data));

        Assert.Equal("The IMAP protocol logger is not initialized", ex.Message);
    }

    [Fact]
    public void Pop3GetLog_WithoutLogger_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() => global::RuriLib.Blocks.Requests.Pop3.Methods.Pop3GetLog(data));

        Assert.Equal("The POP3 protocol logger is not initialized", ex.Message);
    }

    [Fact]
    public void SmtpGetLog_WithoutLogger_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() => global::RuriLib.Blocks.Requests.Smtp.Methods.SmtpGetLog(data));

        Assert.Equal("The SMTP protocol logger is not initialized", ex.Message);
    }

    [Fact]
    public void MailGetLog_WithLogger_ReturnsContent()
    {
        var data = NewBotData();
        data.SetObject("imapLogger", CreateLogger("imap-log"));
        data.SetObject("pop3Logger", CreateLogger("pop3-log"));
        data.SetObject("smtpLogger", CreateLogger("smtp-log"));

        Assert.Equal("imap-log", global::RuriLib.Blocks.Requests.Imap.Methods.ImapGetLog(data));
        Assert.Equal("pop3-log", global::RuriLib.Blocks.Requests.Pop3.Methods.Pop3GetLog(data));
        Assert.Equal("smtp-log", global::RuriLib.Blocks.Requests.Smtp.Methods.SmtpGetLog(data));
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
