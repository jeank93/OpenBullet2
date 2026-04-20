using System;
using System.Collections.Generic;
using System.Text;

namespace RuriLib.Models.Variables;

public class ByteArrayVariable : Variable
{
    private readonly byte[]? value;

    public ByteArrayVariable(byte[]? value)
    {
        this.value = value;
        Type = VariableType.ByteArray;
    }

    public override string AsString() => value is null ? "null" : Encoding.UTF8.GetString(value);

    public override int AsInt() => BitConverter.ToInt32(value ?? throw new InvalidCastException(), 0);

    public override bool AsBool() => BitConverter.ToBoolean(value ?? throw new InvalidCastException(), 0);

    public override byte[]? AsByteArray() => value;

    public override float AsFloat() => AsInt();

    public override List<string> AsListOfStrings() => [AsString()];

    public override object? AsObject() => value;
}
