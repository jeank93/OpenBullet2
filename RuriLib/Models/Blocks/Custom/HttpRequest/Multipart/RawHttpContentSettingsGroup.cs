using RuriLib.Models.Blocks.Settings;

namespace RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;

public class RawHttpContentSettingsGroup : HttpContentSettingsGroup
{
    public BlockSetting Data { get; set; } = BlockSettingFactory.CreateByteArraySetting("data");

    public RawHttpContentSettingsGroup()
    {
        ((StringSetting)ContentType.FixedSetting).Value = "application/octet-stream";
    }
}
