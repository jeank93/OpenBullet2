using Newtonsoft.Json;
using RuriLib.Blocks.Requests.Http;
using RuriLib.Functions.Http;
using RuriLib.Functions.Http.Options;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Models.Proxies;
using RuriLib.Tests.Utils;
using RuriLib.Tests.Utils.Mockup;
using System;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Functions.Http;

[Collection(nameof(ProxyServerCollection))]
public class HttpRequestProxyIntegrationTests
{
    [Theory]
    [InlineData(HttpLibrary.RuriLibHttp, ProxyType.Http)]
    [InlineData(HttpLibrary.RuriLibHttp, ProxyType.Socks4)]
    [InlineData(HttpLibrary.RuriLibHttp, ProxyType.Socks4a)]
    [InlineData(HttpLibrary.RuriLibHttp, ProxyType.Socks5)]
    [InlineData(HttpLibrary.SystemNet, ProxyType.Http)]
    [InlineData(HttpLibrary.SystemNet, ProxyType.Socks4)]
    [InlineData(HttpLibrary.SystemNet, ProxyType.Socks4a)]
    [InlineData(HttpLibrary.SystemNet, ProxyType.Socks5)]
    public async Task HttpRequestStandard_Get_ThroughProxy_Verify(HttpLibrary library, ProxyType proxyType)
    {
        var connection = await TestProxyServer.GetConnectionInfo();
        var proxy = connection.CreateProxy(proxyType);
        var data = NewBotData(proxy);

        var queryValue = $"{library}-{proxyType}".ToLowerInvariant();
        var options = new StandardHttpRequestOptions
        {
            Url = connection.BuildTargetUrl($"anything?proxy={queryValue}"),
            Method = global::RuriLib.Functions.Http.HttpMethod.GET,
            HttpLibrary = library,
            TimeoutMilliseconds = 20000,
            CustomHeaders =
            {
                ["Custom-Proxy-Test"] = queryValue
            }
        };

        await Methods.HttpRequestStandard(data, options);

        var response = DeserializeHttpBinResponse(data.SOURCE);
        var actualUri = new Uri(response.Url);

        Assert.Equal("GET", response.Method);
        Assert.Equal(queryValue, response.Headers["Custom-Proxy-Test"]);
        Assert.Equal("/anything", actualUri.AbsolutePath);
        Assert.Equal($"?proxy={queryValue}", actualUri.Query);
        Assert.Equal(200, data.RESPONSECODE);
    }

    [Theory]
    [InlineData(HttpLibrary.RuriLibHttp, ProxyType.Http)]
    [InlineData(HttpLibrary.RuriLibHttp, ProxyType.Socks5)]
    [InlineData(HttpLibrary.SystemNet, ProxyType.Http)]
    [InlineData(HttpLibrary.SystemNet, ProxyType.Socks5)]
    public async Task HttpRequestStandard_Get_ThroughAuthenticatedProxy_Verify(HttpLibrary library, ProxyType proxyType)
    {
        var connection = await TestProxyServer.GetConnectionInfo();
        var proxy = connection.CreateAuthenticatedProxy(proxyType);
        var data = NewBotData(proxy);

        var queryValue = $"{library}-{proxyType}-auth".ToLowerInvariant();
        var options = new StandardHttpRequestOptions
        {
            Url = connection.BuildTargetUrl($"anything?proxy={queryValue}"),
            Method = global::RuriLib.Functions.Http.HttpMethod.GET,
            HttpLibrary = library,
            TimeoutMilliseconds = 20000,
            CustomHeaders =
            {
                ["Custom-Proxy-Test"] = queryValue
            }
        };

        await Methods.HttpRequestStandard(data, options);

        var response = DeserializeHttpBinResponse(data.SOURCE);
        var actualUri = new Uri(response.Url);

        Assert.Equal("GET", response.Method);
        Assert.Equal(queryValue, response.Headers["Custom-Proxy-Test"]);
        Assert.Equal("/anything", actualUri.AbsolutePath);
        Assert.Equal($"?proxy={queryValue}", actualUri.Query);
        Assert.Equal(200, data.RESPONSECODE);
    }

    private static BotData NewBotData(Proxy proxy)
        => new(
            new global::RuriLib.Models.Bots.Providers(null!)
            {
                ProxySettings = new MockedProxySettingsProvider(),
                Security = new MockedSecurityProvider()
            },
            new ConfigSettings(),
            new BotLogger(),
            new DataLine("proxy-http-test", new WordlistType()),
            proxy,
            useProxy: true)
        {
            CancellationToken = TestContext.Current.CancellationToken
        };

    private static HttpBinResponse DeserializeHttpBinResponse(string source)
        => JsonConvert.DeserializeObject<HttpBinResponse>(source)
           ?? throw new InvalidOperationException("httpbin response could not be deserialized");
}

[CollectionDefinition(nameof(ProxyServerCollection), DisableParallelization = true)]
public class ProxyServerCollection;
