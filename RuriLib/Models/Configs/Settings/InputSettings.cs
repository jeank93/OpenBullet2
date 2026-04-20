using System.Collections.Generic;

namespace RuriLib.Models.Configs.Settings;

public class CustomInput
{
    public string Description { get; set; } = string.Empty;
    public string VariableName { get; set; } = string.Empty;
    public string DefaultAnswer { get; set; } = string.Empty;
}

public class InputSettings
{
    public List<CustomInput> CustomInputs { get; set; } = [];
}
