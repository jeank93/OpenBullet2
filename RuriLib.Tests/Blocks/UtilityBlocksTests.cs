using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils.Mockup;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RuriLib.Tests.Blocks;

public sealed class UtilityBlocksTests : IDisposable
{
    private readonly string tempDir;

    public UtilityBlocksTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "ob2-utility-block-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
    }

    [Fact]
    public void ClearCookies_EmptiesCookieJar()
    {
        var data = NewBotData();
        data.COOKIES["session"] = "abc";

        global::RuriLib.Blocks.Utility.Methods.ClearCookies(data);

        Assert.Empty(data.COOKIES);
    }

    [Fact]
    public void UTF8AndBase64_RoundTrip()
    {
        var data = NewBotData();
        var base64 = global::RuriLib.Blocks.Utility.Conversion.Methods.UTF8ToBase64(data, "ciao");
        var utf8 = global::RuriLib.Blocks.Utility.Conversion.Methods.Base64ToUTF8(data, base64);

        Assert.Equal("ciao", utf8);
    }

    [Fact]
    public void StringToBytes_And_Back_RoundTrip()
    {
        var data = NewBotData();
        var bytes = global::RuriLib.Blocks.Utility.Conversion.Methods.StringToBytes(
            data,
            "hello",
            global::RuriLib.Blocks.Utility.Conversion.StringEncoding.UTF8);
        var value = global::RuriLib.Blocks.Utility.Conversion.Methods.BytesToString(
            data,
            bytes,
            global::RuriLib.Blocks.Utility.Conversion.StringEncoding.UTF8);

        Assert.Equal("hello", value);
    }

    [Fact]
    public void SvgToPng_ReturnsPngBytes()
    {
        var data = NewBotData();
        var png = global::RuriLib.Blocks.Utility.Images.Methods.SvgToPng(
            data,
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\" fill=\"red\"/></svg>",
            10,
            10);

        Assert.True(png.Length > 8);
        Assert.Equal([137, 80, 78, 71, 13, 10, 26, 10], png.Take(8).ToArray());
    }

    [Fact]
    public async System.Threading.Tasks.Task FileWriteReadAppend_And_List_Work()
    {
        var data = NewBotData();
        var filePath = Path.Combine(tempDir, "nested", "test.txt");

        await global::RuriLib.Blocks.Utility.Files.Methods.FileWrite(data, filePath, "line1");
        await global::RuriLib.Blocks.Utility.Files.Methods.FileAppendLines(data, filePath, ["line2", "line3"]);

        var text = await global::RuriLib.Blocks.Utility.Files.Methods.FileRead(data, filePath);
        var lines = await global::RuriLib.Blocks.Utility.Files.Methods.FileReadLines(data, filePath);
        var exists = global::RuriLib.Blocks.Utility.Files.Methods.FileExists(data, filePath);
        var folderExists = global::RuriLib.Blocks.Utility.Files.Methods.FolderExists(data, Path.GetDirectoryName(filePath)!);
        var files = global::RuriLib.Blocks.Utility.Files.Methods.GetFilesInFolder(data, Path.GetDirectoryName(filePath)!);

        Assert.True(exists);
        Assert.True(folderExists);
        Assert.Contains("line1", text);
        Assert.Equal(["line1line2", "line3"], lines);
        Assert.Contains(filePath, files);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
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
}
