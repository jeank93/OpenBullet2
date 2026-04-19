using RuriLib.Providers.Captchas;
using RuriLib.Providers.Emails;
using RuriLib.Providers.Proxies;
using RuriLib.Providers.Puppeteer;
using RuriLib.Providers.RandomNumbers;
using RuriLib.Providers.Security;
using RuriLib.Providers.Selenium;
using RuriLib.Providers.UserAgents;
using RuriLib.Services;

namespace RuriLib.Models.Bots;

/// <summary>
/// The whole purpose of this class is to hide RuriLib settings from the config.
/// It's probably overengineered right now but at least it's future-proof.
/// </summary>
public class Providers
{
    public IRandomUAProvider RandomUA { get; set; } = null!;
    public ICaptchaProvider Captcha { get; set; } = null!;
    public IEmailDomainRepository EmailDomains { get; set; } = null!;
    public IRNGProvider RNG { get; set; }
    public IPuppeteerBrowserProvider PuppeteerBrowser { get; set; } = null!;
    public ISeleniumBrowserProvider SeleniumBrowser { get; set; } = null!;
    public IGeneralSettingsProvider GeneralSettings { get; set; } = null!;
    public IProxySettingsProvider ProxySettings { get; set; } = null!;
    public ISecurityProvider Security { get; set; } = null!;

    /// <summary>
    /// Initializes all default providers.
    /// </summary>
    public Providers(RuriLibSettingsService? settings)
    {
        if (settings is not null)
        {
            RandomUA = new DefaultRandomUAProvider(settings);
            EmailDomains = new FileEmailDomainRepository();
            Captcha = new CaptchaSharpProvider(settings);
            PuppeteerBrowser = new DefaultPuppeteerBrowserProvider(settings);
            SeleniumBrowser = new DefaultSeleniumBrowserProvider(settings);
            GeneralSettings = new DefaultGeneralSettingsProvider(settings);
            ProxySettings = new DefaultProxySettingsProvider(settings);
            Security = new DefaultSecurityProvider(settings);
        }

        RNG = new DefaultRNGProvider();
    }
}
