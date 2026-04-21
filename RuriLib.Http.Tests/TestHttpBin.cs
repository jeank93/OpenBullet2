using System;

namespace RuriLib.Http.Tests;

internal static class TestHttpBin
{
    private static readonly string DefaultBaseUrl = "https://httpbin.org";
    private static string BaseUrl => Environment.GetEnvironmentVariable("OB2_HTTPBIN_BASE_URL") ?? DefaultBaseUrl;

    public static Uri BuildUri(string relativePath)
        => new($"{BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}");

    public static Uri BuildHttpUri(string relativePath)
        => BuildUri(relativePath);

    public static Uri BuildCompressedUri(string relativePath)
        => BuildUri(relativePath);

    public static Uri HttpCookieScopeUri()
        => new(BaseUrl);
}
