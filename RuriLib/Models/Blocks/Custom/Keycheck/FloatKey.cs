using RuriLib.Models.Blocks.Settings;
using RuriLib.Models.Conditions.Comparisons;

namespace RuriLib.Models.Blocks.Custom.Keycheck;

public class FloatKey : Key
{
    public NumComparison Comparison { get; set; } = NumComparison.EqualTo;

    public FloatKey()
    {
        Left = BlockSettingFactory.CreateFloatSetting(string.Empty, mode: SettingInputMode.Variable);
        Right = BlockSettingFactory.CreateFloatSetting(string.Empty);
    }
}
