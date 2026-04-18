using RuriLib.Models.Blocks.Settings;
using System;

namespace RuriLib.Models.Blocks.Parameters;

/// <summary>
/// A parameter of type enum.
/// </summary>
public class EnumParameter : BlockParameter
{
    /// <summary>
    /// The type of the enum.
    /// </summary>
    public Type EnumType { get; set; }

    /// <summary>
    /// The default value of the parameter.
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    /// The available options for the parameter.
    /// </summary>
    public string[] Options => Enum.GetNames(EnumType);

    /// <summary></summary>
    public EnumParameter(string name, Type enumType, string defaultValue) : base(name)
    {
        EnumType = enumType;
        DefaultValue = defaultValue;
    }

    /// <inheritdoc />
    public override BlockSetting ToBlockSetting()
        => BlockSettingFactory.CreateEnumSetting(Name, EnumType, DefaultValue, InputMode,
            DefaultVariableName, PrettyName, Description);
}
