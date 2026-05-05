using OpenBullet2.Core.Services;
using RuriLib.Models.Configs;
using System;

namespace OpenBullet2.Native.ViewModels;

public class ConfigReadmeViewModel : ViewModelBase
{
    private readonly ConfigService configService;
    private Config Config => configService.SelectedConfig
        ?? throw new InvalidOperationException("No config selected");

    public string Readme
    {
        get => Config.Readme;
        set
        {
            Config.Readme = value;
            OnPropertyChanged();
        }
    }

    public ConfigReadmeViewModel(ConfigService configService) => this.configService = configService;
}
