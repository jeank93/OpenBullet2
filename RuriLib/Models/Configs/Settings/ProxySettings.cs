using RuriLib.Models.Proxies;

namespace RuriLib.Models.Configs.Settings;

public class ProxySettings
{
    public bool UseProxies { get; set; }

    public int MaxUsesPerProxy { get; set; }

    public int BanLoopEvasion { get; set; } = 100;

    public string[] BanProxyStatuses { get; set; } = ["BAN", "ERROR"];

    public ProxyType[] AllowedProxyTypes { get; set; } =
    [
        ProxyType.Http,
        ProxyType.Socks4,
        ProxyType.Socks4a,
        ProxyType.Socks5
    ];
}
