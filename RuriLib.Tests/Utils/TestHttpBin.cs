using System;

namespace RuriLib.Tests.Utils;

internal static class TestHttpBin
{
    private static readonly string DefaultBaseUrl = "https://httpbin.org";

    public static string BaseUrl => Environment.GetEnvironmentVariable("OB2_HTTPBIN_BASE_URL") ?? DefaultBaseUrl;

    public static string BuildUrl(string relativePath)
        => $"{BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";

    public static string HostHeader => new Uri(BaseUrl).Authority;
}
