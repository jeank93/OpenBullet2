using RuriLib.Models.Blocks.Settings;
using RuriLib.Models.Conditions.Comparisons;

namespace RuriLib.Models.Blocks.Custom.Keycheck;

public class IntKey : Key
{
    public NumComparison Comparison { get; set; } = NumComparison.EqualTo;

    public IntKey()
    {
        Left = BlockSettingFactory.CreateIntSetting(string.Empty, mode: SettingInputMode.Variable,
            defaultVariableName: "data.RESPONSECODE");
        Right = BlockSettingFactory.CreateIntSetting(string.Empty);
    }
}
