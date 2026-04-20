using RuriLib.Models.Blocks.Settings;

namespace RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;

public class StringHttpContentSettingsGroup : HttpContentSettingsGroup
{
    public BlockSetting Data { get; set; } = BlockSettingFactory.CreateStringSetting("data");

    public StringHttpContentSettingsGroup()
    {
        if (ContentType.FixedSetting is StringSetting contentTypeSetting)
        {
            contentTypeSetting.Value = "text/plain";
        }
    }
}
