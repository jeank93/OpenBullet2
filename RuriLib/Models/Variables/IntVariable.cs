using System;
using System.Collections.Generic;

namespace RuriLib.Models.Variables;

public class IntVariable : Variable
{
    private readonly int value;

    public IntVariable(int value)
    {
        this.value = value;
        Type = VariableType.Int;
    }

    public override string AsString() => value.ToString();

    public override int AsInt() => value;

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
