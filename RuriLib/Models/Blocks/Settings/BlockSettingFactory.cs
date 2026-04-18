using RuriLib.Extensions;
using RuriLib.Models.Blocks.Settings.Interpolated;
using System;
using System.Collections.Generic;

namespace RuriLib.Models.Blocks.Settings;

/// <summary>
/// Creates <see cref="BlockSetting"/> instances for the supported parameter types.
/// </summary>
public static class BlockSettingFactory
{
    /// <summary>
    /// Creates a boolean block setting.
    /// </summary>
    public static BlockSetting CreateBoolSetting(string name, bool defaultValue = false,
        SettingInputMode mode = SettingInputMode.Fixed, string? defaultVariableName = null,
        string? readableName = null, string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            ReadableName = readableName ?? name.ToReadableName(),
            InputMode = mode,
            InputVariableName = defaultVariableName ?? string.Empty,
            FixedSetting = new BoolSetting { Value = defaultValue }
        };

    /// <summary>
    /// Creates an integer block setting.
    /// </summary>
    public static BlockSetting CreateIntSetting(string name, int defaultValue = 0,
        SettingInputMode mode = SettingInputMode.Fixed, string? defaultVariableName = null,
        string? readableName = null, string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            ReadableName = readableName ?? name.ToReadableName(),
            InputMode = mode,
            InputVariableName = defaultVariableName ?? string.Empty,
            FixedSetting = new IntSetting { Value = defaultValue }
        };

    /// <summary>
    /// Creates a float block setting.
    /// </summary>
    public static BlockSetting CreateFloatSetting(string name, float defaultValue = 0,
        SettingInputMode mode = SettingInputMode.Fixed, string? defaultVariableName = null,
        string? readableName = null, string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            ReadableName = readableName ?? name.ToReadableName(),
            InputMode = mode,
            InputVariableName = defaultVariableName ?? string.Empty,
            FixedSetting = new FloatSetting { Value = defaultValue }
        };

    /// <summary>
    /// Creates a byte array block setting.
    /// </summary>
    public static BlockSetting CreateByteArraySetting(string name, byte[]? defaultValue = null,
        SettingInputMode mode = SettingInputMode.Fixed, string? defaultVariableName = null,
        string? readableName = null, string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            ReadableName = readableName ?? name.ToReadableName(),
            InputMode = mode,
            InputVariableName = defaultVariableName ?? string.Empty,
            FixedSetting = new ByteArraySetting { Value = defaultValue ?? [] }
        };

    /// <summary>
    /// Creates an enum block setting for the given enum type parameter.
    /// </summary>
    public static BlockSetting CreateEnumSetting<T>(string name, string defaultValue = "",
        SettingInputMode mode = SettingInputMode.Fixed, string? defaultVariableName = null,
        string? readableName = null, string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            ReadableName = readableName ?? name.ToReadableName(),
            InputMode = mode,
            InputVariableName = defaultVariableName ?? string.Empty,
            FixedSetting = new EnumSetting(typeof(T)) { Value = defaultValue }
        };

    /// <summary>
    /// Creates an enum block setting for the provided runtime enum type.
    /// </summary>
    public static BlockSetting CreateEnumSetting(string name, Type enumType, string defaultValue = "",
        SettingInputMode mode = SettingInputMode.Fixed, string? defaultVariableName = null,
        string? readableName = null, string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            ReadableName = readableName ?? name.ToReadableName(),
            InputMode = mode,
            InputVariableName = defaultVariableName ?? string.Empty,
            FixedSetting = new EnumSetting(enumType) { Value = defaultValue }
        };

    /// <summary>
    /// Creates a string block setting.
    /// </summary>
    public static BlockSetting CreateStringSetting(string name, string? defaultValue = "",
        SettingInputMode mode = SettingInputMode.Fixed, bool multiLine = false,
        string? readableName = null, string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            ReadableName = readableName ?? name.ToReadableName(),
            InputMode = mode,
            InputVariableName = defaultValue ?? string.Empty,
            InterpolatedSetting = new InterpolatedStringSetting
            {
                Value = defaultValue,
                MultiLine = multiLine
            },
            FixedSetting = new StringSetting
            {
                Value = defaultValue,
                MultiLine = multiLine
            }
        };

    /// <summary>
    /// Creates a list-of-strings block setting.
    /// </summary>
    public static BlockSetting CreateListOfStringsSetting(string name, List<string>? defaultValue = null,
        SettingInputMode mode = SettingInputMode.Fixed, string? readableName = null,
        string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            ReadableName = readableName ?? name.ToReadableName(),
            InputMode = mode,
            InterpolatedSetting = new InterpolatedListOfStringsSetting
            {
                Value = defaultValue ?? []
            },
            FixedSetting = new ListOfStringsSetting
            {
                Value = defaultValue ?? []
            }
        };

    /// <summary>
    /// Creates a variable-backed list-of-strings block setting.
    /// </summary>
    public static BlockSetting CreateListOfStringsSetting(string name, string variableName,
        string? readableName = null, string? description = null)
    {
        var setting = CreateListOfStringsSetting(name, null, SettingInputMode.Variable,
            readableName, description);
        setting.InputVariableName = variableName;
        return setting;
    }

    /// <summary>
    /// Creates a dictionary-of-strings block setting.
    /// </summary>
    public static BlockSetting CreateDictionaryOfStringsSetting(string name,
        Dictionary<string, string>? defaultValue = null,
        SettingInputMode mode = SettingInputMode.Fixed, string? readableName = null,
        string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            ReadableName = readableName ?? name.ToReadableName(),
            InputMode = mode,
            InterpolatedSetting = new InterpolatedDictionaryOfStringsSetting
            {
                Value = defaultValue ?? []
            },
            FixedSetting = new DictionaryOfStringsSetting
            {
                Value = defaultValue ?? []
            }
        };

    /// <summary>
    /// Creates a variable-backed dictionary-of-strings block setting.
    /// </summary>
    public static BlockSetting CreateDictionaryOfStringsSetting(string name, string variableName,
        string? readableName = null, string? description = null)
    {
        var setting = CreateDictionaryOfStringsSetting(name, null, SettingInputMode.Variable,
            readableName, description);
        setting.InputVariableName = variableName;
        return setting;
    }
}
