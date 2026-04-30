using RuriLib.Helpers.CSharp;
using RuriLib.Models.Blocks.Settings;
using Xunit;

namespace RuriLib.Tests.Helpers.CSharp;

public class CSharpWriterTests
{
    [Fact]
    public void SerializeInterpString_Alone_ReplaceCorrectly()
    {
        Assert.Equal("$\"{value}\"", CSharpWriter.SerializeInterpString("<value>"));
    }

    [Fact]
    public void SerializeInterpString_Surrounded_ReplaceCorrectly()
    {
        Assert.Equal("$\"my {value} is cool\"", CSharpWriter.SerializeInterpString("my <value> is cool"));
    }

    [Fact]
    public void SerializeInterpString_SingleCharacter_ReplaceCorrectly()
    {
        Assert.Equal("$\"my {a} is cool\"", CSharpWriter.SerializeInterpString("my <a> is cool"));
    }

    [Fact]
    public void SerializeByteArray_Null_ReturnsNullLiteral()
    {
        Assert.Equal("null", CSharpWriter.SerializeByteArray(null));
    }

    [Fact]
    public void SerializeList_Null_ReturnsNullLiteral()
    {
        Assert.Equal("null", CSharpWriter.SerializeList(null));
    }

    [Fact]
    public void SerializeDictionary_Null_ReturnsNullLiteral()
    {
        Assert.Equal("null", CSharpWriter.SerializeDictionary(null));
    }

    [Fact]
    public void ToPrimitive_Null_ReturnsNullLiteral()
    {
        Assert.Equal("null", CSharpWriter.ToPrimitive(null));
    }

    [Fact]
    public void FromSetting_GlobalVariable_UsesDynamicHelperCall()
    {
        var setting = new BlockSetting
        {
            InputMode = SettingInputMode.Variable,
            InputVariableName = "globals.myVar",
            FixedSetting = new StringSetting()
        };

        Assert.Equal("ObjectExtensions.DynamicAsString(globals.myVar)", CSharpWriter.FromSetting(setting));
    }

    [Fact]
    public void FromSetting_InputVariable_UsesDynamicHelperCall()
    {
        var setting = new BlockSetting
        {
            InputMode = SettingInputMode.Variable,
            InputVariableName = "input.count",
            FixedSetting = new IntSetting()
        };

        Assert.Equal("ObjectExtensions.DynamicAsInt(input.count)", CSharpWriter.FromSetting(setting));
    }

    [Fact]
    public void FromSetting_NormalVariable_UsesRegularExtensionCall()
    {
        var setting = new BlockSetting
        {
            InputMode = SettingInputMode.Variable,
            InputVariableName = "myVar",
            FixedSetting = new StringSetting()
        };

        Assert.Equal("myVar.AsString()", CSharpWriter.FromSetting(setting));
    }
}
