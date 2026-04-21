using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils.Mockup;
using Xunit;

namespace RuriLib.Tests.Blocks;

public class BrowserBlocksTests
{
    [Fact]
    public void SeleniumReload_WithoutBrowser_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() =>
            global::RuriLib.Blocks.Selenium.Browser.Methods.SeleniumReload(data));

        Assert.Equal("The browser is not open!", ex.Message);
    }

    [Fact]
    public void SeleniumGetWidth_WithoutBrowser_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() =>
            global::RuriLib.Blocks.Selenium.Elements.Methods.SeleniumGetWidth(
                data,
                global::RuriLib.Functions.Puppeteer.FindElementBy.Id,
                "main",
                0));

        Assert.Equal("The browser is not open!", ex.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task PuppeteerReload_WithoutBrowser_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            global::RuriLib.Blocks.Puppeteer.Browser.Methods.PuppeteerReload(data));

        Assert.Equal("No pages open!", ex.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task PuppeteerGetWidth_WithoutBrowser_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            global::RuriLib.Blocks.Puppeteer.Elements.Methods.PuppeteerGetWidth(
                data,
                global::RuriLib.Functions.Puppeteer.FindElementBy.Id,
                "main",
                0));

        Assert.Equal("No pages open!", ex.Message);
    }

    [Fact]
    public void SeleniumNavigateTo_WithoutBrowser_Throws()
    {
        var data = NewBotData();

        var ex = Assert.Throws<BlockExecutionException>(() =>
            global::RuriLib.Blocks.Selenium.Page.Methods.SeleniumNavigateTo(data));

        Assert.Equal("The browser is not open!", ex.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task PuppeteerNavigateTo_WithoutBrowser_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            global::RuriLib.Blocks.Puppeteer.Page.Methods.PuppeteerNavigateTo(data));

        Assert.Equal("No pages open!", ex.Message);
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
