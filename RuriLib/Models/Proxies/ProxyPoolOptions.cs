namespace RuriLib.Models.Proxies;

public class ProxyPoolOptions
{
    public ProxyType[] AllowedTypes { get; set; } = [ProxyType.Http, ProxyType.Socks4, ProxyType.Socks5];
}
