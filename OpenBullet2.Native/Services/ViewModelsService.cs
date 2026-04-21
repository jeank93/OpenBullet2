using OpenBullet2.Native.ViewModels;

namespace OpenBullet2.Native.Services;

public class ViewModelsService
{
    public JobsViewModel Jobs { get; set; } = new();
    public ProxiesViewModel Proxies { get; set; } = new();
    public WordlistsViewModel Wordlists { get; set; } = new();
    public ConfigsViewModel Configs { get; set; } = new();
    public HitsViewModel Hits { get; set; } = new();
    public OBSettingsViewModel OBSettings { get; set; } = new();
    public RLSettingsViewModel RLSettings { get; set; } = new();
    public PluginsViewModel Plugins { get; set; } = new();

    public ConfigMetadataViewModel ConfigMetadata { get; set; } = new();
    public ConfigReadmeViewModel ConfigReadme { get; set; } = new();
    public ConfigStackerViewModel ConfigStacker { get; set; } = new();
    public ConfigSettingsViewModel ConfigSettings { get; set; } = new();

    public DebuggerViewModel Debugger { get; set; } = new();
}
