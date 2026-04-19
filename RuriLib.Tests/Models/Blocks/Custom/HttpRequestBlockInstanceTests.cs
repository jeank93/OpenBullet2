using System;
using RuriLib.Exceptions;
using RuriLib.Helpers.Blocks;
using RuriLib.Models.Blocks.Custom;
using RuriLib.Models.Blocks.Custom.HttpRequest;
using RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;
using RuriLib.Models.Blocks.Settings;
using RuriLib.Models.Configs;
using Xunit;

namespace RuriLib.Tests.Models.Blocks.Custom;

public class HttpRequestBlockInstanceTests
{
    private readonly string _nl = Environment.NewLine;

    /*
    [Fact]
    public void ToLC_StandardPost_OutputScript()
    {
        var repo = new DescriptorsRepository();
        var descriptor = repo.GetAs<HttpRequestBlockDescriptor>("HttpRequest");
        var block = new HttpRequestBlockInstance(descriptor);
        ...
    }
    */

    [Fact]
    public void ToLC_StandardPost_OutputScript()
    {
        var block = BlockFactory.GetBlock<HttpRequestBlockInstance>("HttpRequest");

        var url = block.Settings["url"];
        url.InputMode = SettingInputMode.Fixed;
        (url.FixedSetting as StringSetting)!.Value = "https://example.com";

        var method = block.Settings["method"];
        method.InputMode = SettingInputMode.Fixed;
        (method.FixedSetting as EnumSetting)!.Value = "POST";

        block.RequestParams = new StandardRequestParams
        {
            Content = BlockSettingFactory.CreateStringSetting(string.Empty, "key1=value1&key2=value2", SettingInputMode.Fixed),
            ContentType = BlockSettingFactory.CreateStringSetting(string.Empty, "application/x-www-form-urlencoded", SettingInputMode.Fixed)
        };

        var output = block.ToLC();

        Assert.Contains($"  url = \"https://example.com\"{_nl}", output);
        Assert.Contains($"  method = POST{_nl}", output);
        Assert.Contains($"  TYPE:STANDARD{_nl}", output);
        Assert.Contains($"  \"key1=value1&key2=value2\"{_nl}", output);
        Assert.Contains($"  \"application/x-www-form-urlencoded\"{_nl}", output);
    }

    [Fact]
    public void FromLC_MultipartPost_BuildBlock()
    {
        var block = BlockFactory.GetBlock<HttpRequestBlockInstance>("HttpRequest");
        var script = $"  url = \"https://example.com\"{_nl}  method = POST{_nl}  TYPE:MULTIPART{_nl}  @myBoundary{_nl}  CONTENT:STRING \"stringName\" \"stringContent\" \"stringContentType\"{_nl}  CONTENT:FILE \"fileName\" \"file.txt\" \"fileContentType\"{_nl}";
        var lineNumber = 0;

        block.FromLC(ref script, ref lineNumber);

        Assert.Equal("https://example.com", Assert.IsType<StringSetting>(block.Settings["url"].FixedSetting).Value);
        Assert.Equal("POST", Assert.IsType<EnumSetting>(block.Settings["method"].FixedSetting).Value);

        var multipart = Assert.IsType<MultipartRequestParams>(block.RequestParams);
        Assert.Equal(SettingInputMode.Variable, multipart.Boundary.InputMode);
        Assert.Equal("myBoundary", multipart.Boundary.InputVariableName);

        var stringContent = Assert.IsType<StringHttpContentSettingsGroup>(multipart.Contents[0]);
        Assert.Equal("stringName", Assert.IsType<StringSetting>(stringContent.Name.FixedSetting).Value);
        Assert.Equal("stringContent", Assert.IsType<StringSetting>(stringContent.Data.FixedSetting).Value);
        Assert.Equal("stringContentType", Assert.IsType<StringSetting>(stringContent.ContentType.FixedSetting).Value);

        var fileContent = Assert.IsType<FileHttpContentSettingsGroup>(multipart.Contents[1]);
        Assert.Equal("fileName", Assert.IsType<StringSetting>(fileContent.Name.FixedSetting).Value);
        Assert.Equal("file.txt", Assert.IsType<StringSetting>(fileContent.FileName.FixedSetting).Value);
        Assert.Equal("fileContentType", Assert.IsType<StringSetting>(fileContent.ContentType.FixedSetting).Value);
    }

    [Fact]
    public void FromLC_MultipartWithoutBoundary_Throws()
    {
        var block = BlockFactory.GetBlock<HttpRequestBlockInstance>("HttpRequest");
        var script = $"  TYPE:MULTIPART{_nl}";
        var lineNumber = 0;

        Assert.Throws<LoliCodeParsingException>(() => block.FromLC(ref script, ref lineNumber));
    }

    [Fact]
    public void FromLC_ContentWithoutMultipart_Throws()
    {
        var block = BlockFactory.GetBlock<HttpRequestBlockInstance>("HttpRequest");
        var script = $"  CONTENT:STRING \"name\" \"content\" \"contentType\"{_nl}";
        var lineNumber = 0;

        Assert.Throws<LoliCodeParsingException>(() => block.FromLC(ref script, ref lineNumber));
    }

    [Fact]
    public void ToCSharp_MultipartPost_OutputScript()
    {
        var block = BlockFactory.GetBlock<HttpRequestBlockInstance>("HttpRequest");
        var script = $"  url = \"https://example.com\"{_nl}  method = POST{_nl}  TYPE:MULTIPART{_nl}  @myBoundary{_nl}  CONTENT:STRING \"stringName\" \"stringContent\" \"stringContentType\"{_nl}  CONTENT:FILE \"fileName\" \"file.txt\" \"fileContentType\"{_nl}";
        var lineNumber = 0;
        block.FromLC(ref script, ref lineNumber);

        var output = block.ToCSharp([], new ConfigSettings());

        Assert.Contains("await HttpRequestMultipart(data, new MultipartHttpRequestOptions {", output);
        Assert.Contains("Boundary = myBoundary.AsString()", output);
        Assert.Contains("new StringHttpContent(\"stringName\", \"stringContent\", \"stringContentType\")", output);
        Assert.Contains("new FileHttpContent(\"fileName\", \"file.txt\", \"fileContentType\")", output);
        Assert.Contains("Url = \"https://example.com\"", output);
        Assert.Contains("Method = RuriLib.Functions.Http.HttpMethod.POST", output);
        Assert.EndsWith("}).ConfigureAwait(false);" + _nl, output);
    }
}
