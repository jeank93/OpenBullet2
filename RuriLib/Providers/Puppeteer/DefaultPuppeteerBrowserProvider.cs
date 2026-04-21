using RuriLib.Services;

namespace RuriLib.Providers.Puppeteer;

/// <summary>
/// Default implementation of <see cref="IPuppeteerBrowserProvider"/>.
/// </summary>
public class DefaultPuppeteerBrowserProvider : IPuppeteerBrowserProvider
{
    /// <summary>
    /// Gets the Chrome binary location.
    /// </summary>
    public string ChromeBinaryLocation { get; }

    /// <summary>
    /// Creates a provider from the persisted RuriLib settings.
    /// </summary>
    public DefaultPuppeteerBrowserProvider(RuriLibSettingsService settings)
    {
        ChromeBinaryLocation = settings.RuriLibSettings.PuppeteerSettings.ChromeBinaryLocation;
    }
}
