using Newtonsoft.Json.Linq;
using RuriLib.Functions.Http;
using RuriLib.Http.Models;
using RuriLib.Models.Proxies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Functions.Networking;

public static class DnsLookup
{
    /// <summary>
    /// Retrieves a list of records from Google's DNS over HTTP service at dns.google.com.
    /// The list is ordered by priority.
    /// </summary>
    public static async Task<List<string>> FromGoogleAsync(
        string domain,
        string type,
        Proxy? proxy = null,
        int timeout = 30000,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(type);

        var url = $"https://dns.google.com/resolve?name={Uri.EscapeDataString(domain)}&type={type}";

        using var httpClient = HttpFactory.GetRLHttpClient(proxy, new HttpOptions
        {
            ConnectTimeout = TimeSpan.FromMilliseconds(timeout),
            ReadWriteTimeout = TimeSpan.FromMilliseconds(timeout)
        });

        using var request = new HttpRequest
        {
            Uri = new Uri(url)
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var json = await (response.Content?.ReadAsStringAsync(cancellationToken)
            ?? Task.FromResult(string.Empty));
        var answers = JObject.Parse(json)["Answer"] as JArray;

        if (answers is null)
        {
            return [];
        }

        return answers
            .Select(ParseAnswer)
            .Where(record => record.HasValue)
            .Select(record => record!.Value)
            .OrderBy(record => record.Priority)
            .Select(record => record.Value)
            .ToList();
    }

    private static (int Priority, string Value)? ParseAnswer(JToken token)
    {
        var data = token.Value<string>("data");

        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        var parts = data.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2 && int.TryParse(parts[0], out var priority))
        {
            return (priority, parts[1].TrimEnd('.'));
        }

        return (int.MaxValue, data.TrimEnd('.'));
    }
}
