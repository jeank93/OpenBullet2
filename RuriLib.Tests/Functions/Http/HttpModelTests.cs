using RuriLib.Functions.Http;
using RuriLib.Functions.Http.Options;
using RuriLib.Functions.Networking;
using Xunit;

namespace RuriLib.Tests.Functions.Http;

public class HttpModelTests
{
    [Fact]
    public void HttpOptions_HaveExpectedDefaults()
    {
        var options = new HttpOptions();

        Assert.Equal(5, options.ConnectTimeout.TotalSeconds);
        Assert.Equal(10, options.ReadWriteTimeout.TotalSeconds);
        Assert.True(options.AutoRedirect);
        Assert.Equal(8, options.MaxNumberOfRedirects);
        Assert.True(options.ReadResponseContent);
        Assert.Equal(SecurityProtocol.SystemDefault, options.SecurityProtocol);
        Assert.False(options.UseCustomCipherSuites);
        Assert.NotEmpty(options.CustomCipherSuites);
    }

    [Fact]
    public void HttpRequestOptions_HaveExpectedDefaults()
    {
        var options = new HttpRequestOptions();

        Assert.Equal(string.Empty, options.Url);
        Assert.Equal(HttpMethod.GET, options.Method);
        Assert.True(options.AutoRedirect);
        Assert.Equal(8, options.MaxNumberOfRedirects);
        Assert.Equal(HttpLibrary.RuriLibHttp, options.HttpLibrary);
        Assert.Equal(SecurityProtocol.SystemDefault, options.SecurityProtocol);
        Assert.Empty(options.CustomCookies);
        Assert.Empty(options.CustomHeaders);
        Assert.Equal(10000, options.TimeoutMilliseconds);
        Assert.Equal("1.1", options.HttpVersion);
        Assert.False(options.UseCustomCipherSuites);
        Assert.Empty(options.CustomCipherSuites);
        Assert.Equal(string.Empty, options.CodePagesEncoding);
        Assert.False(options.AlwaysSendContent);
        Assert.False(options.DecodeHtml);
        Assert.True(options.ReadResponseContent);
    }

    [Fact]
    public void HttpRequestDerivedOptions_HaveExpectedDefaults()
    {
        var standard = new StandardHttpRequestOptions();
        var raw = new RawHttpRequestOptions();
        var basicAuth = new BasicAuthHttpRequestOptions();
        var multipart = new MultipartHttpRequestOptions();

        Assert.Equal(string.Empty, standard.Content);
        Assert.Equal(string.Empty, standard.ContentType);
        Assert.False(standard.UrlEncodeContent);

        Assert.Empty(raw.Content);
        Assert.Equal(string.Empty, raw.ContentType);

        Assert.Equal(string.Empty, basicAuth.Username);
        Assert.Equal(string.Empty, basicAuth.Password);

        Assert.Equal(string.Empty, multipart.Boundary);
        Assert.Empty(multipart.Contents);
    }

    [Fact]
    public void HostEntry_ConstructorAssignsHostAndPort()
    {
        var entry = new HostEntry("example.com", 443);

        Assert.Equal("example.com", entry.Host);
        Assert.Equal(443, entry.Port);
    }
}
