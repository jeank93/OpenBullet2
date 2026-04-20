using RuriLib.Models.Blocks.Settings;

namespace RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;

public class RawHttpContentSettingsGroup : HttpContentSettingsGroup
{
    public BlockSetting Data { get; set; } = BlockSettingFactory.CreateByteArraySetting("data");

    public RawHttpContentSettingsGroup()
    {
        if (ContentType.FixedSetting is StringSetting contentTypeSetting)
        {
            contentTypeSetting.Value = "application/octet-stream";
        }
    }
}
