using RuriLib.Models.Blocks.Settings;

namespace RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;

public class StringHttpContentSettingsGroup : HttpContentSettingsGroup
{
    public BlockSetting Data { get; set; } = BlockSettingFactory.CreateStringSetting("data");

    public StringHttpContentSettingsGroup()
    {
        ((StringSetting)ContentType.FixedSetting).Value = "text/plain";
    }
}
