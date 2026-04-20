using RuriLib.Models.Settings;
using RuriLib.Services;
using System;
using System.Linq;

namespace RuriLib.Providers.Proxies;

public class DefaultProxySettingsProvider : IProxySettingsProvider
{
    private readonly ProxySettings settings;

    public DefaultProxySettingsProvider(RuriLibSettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        this.settings = settings.RuriLibSettings.ProxySettings;
    }

    public TimeSpan ConnectTimeout => TimeSpan.FromMilliseconds(settings.ProxyConnectTimeoutMilliseconds);

    public TimeSpan ReadWriteTimeout => TimeSpan.FromMilliseconds(settings.ProxyReadWriteTimeoutMilliseconds);

    public bool ContainsBanKey(string text, out string matchedKey, bool caseSensitive = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            matchedKey = string.Empty;
            return false;
        }

        matchedKey = settings.GlobalBanKeys
            .FirstOrDefault(k => !string.IsNullOrEmpty(k) && text.Contains(k,
                caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;

        return matchedKey.Length != 0;
    }

    public bool ContainsRetryKey(string text, out string matchedKey, bool caseSensitive = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            matchedKey = string.Empty;
            return false;
        }

        matchedKey = settings.GlobalRetryKeys
            .FirstOrDefault(k => !string.IsNullOrEmpty(k) && text.Contains(k,
                caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;

        return matchedKey.Length != 0;
    }
}
