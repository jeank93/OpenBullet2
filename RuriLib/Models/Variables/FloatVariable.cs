using System;
using System.Collections.Generic;
using System.Globalization;

namespace RuriLib.Models.Variables;

public class FloatVariable : Variable
{
    private readonly float value;

    public FloatVariable(float value)
    {
        this.value = value;
        Type = VariableType.Float;
    }

    public override string AsString() => value.ToString(CultureInfo.InvariantCulture);

    public override int AsInt() => (int)value;

    public override bool AsBool() => value switch
    {
        0 => false,
        1 => true,
        _ => throw new InvalidCastException()
    };

    public override byte[] AsByteArray() => BitConverter.GetBytes(value);

    public override float AsFloat() => value;

    public override List<string> AsListOfStrings() => [AsString()];

    public override object AsObject() => value;
}
