namespace RuriLib.Models.Settings;

public class GlobalSettings
{
    public GeneralSettings GeneralSettings { get; set; } = new();
    public CaptchaSettings CaptchaSettings { get; set; } = new();
    public ProxySettings ProxySettings { get; set; } = new();
    public PuppeteerSettings PuppeteerSettings { get; set; } = new();
    public SeleniumSettings SeleniumSettings { get; set; } = new();
}
