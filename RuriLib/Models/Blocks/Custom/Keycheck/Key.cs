using RuriLib.Models.Blocks.Settings;

namespace RuriLib.Models.Blocks.Custom.Keycheck;

public class Key
{
    public BlockSetting Left { get; set; } = BlockSettingFactory.CreateStringSetting(string.Empty);
    public BlockSetting Right { get; set; } = BlockSettingFactory.CreateStringSetting(string.Empty);
}
