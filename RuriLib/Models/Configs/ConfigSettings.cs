using RuriLib.Models.Configs.Settings;
using System.Text.Json.Serialization;

namespace RuriLib.Models.Configs;

public class ConfigSettings
{
    public GeneralSettings GeneralSettings { get; set; } = new();
    public ProxySettings ProxySettings { get; set; } = new();
    public InputSettings InputSettings { get; set; } = new();
    public DataSettings DataSettings { get; set; } = new();

    [JsonPropertyName("PuppeteerSettings")] // For backwards compatibility
    public BrowserSettings BrowserSettings { get; set; } = new();

    public ScriptSettings ScriptSettings { get; set; } = new();
}
