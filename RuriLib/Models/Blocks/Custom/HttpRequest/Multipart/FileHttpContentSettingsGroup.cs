using RuriLib.Models.Blocks.Settings;

namespace RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;

public class FileHttpContentSettingsGroup : HttpContentSettingsGroup
{
    public BlockSetting FileName { get; set; } = BlockSettingFactory.CreateStringSetting("fileName");

    public FileHttpContentSettingsGroup()
    {
        ((StringSetting)ContentType.FixedSetting).Value = "application/octet-stream";
    }
}
