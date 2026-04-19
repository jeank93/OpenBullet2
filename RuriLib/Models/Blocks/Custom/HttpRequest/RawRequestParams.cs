using RuriLib.Models.Blocks.Settings;

namespace RuriLib.Models.Blocks.Custom.HttpRequest;

public class RawRequestParams : RequestParams
{
    public BlockSetting Content { get; set; } = BlockSettingFactory.CreateByteArraySetting("content");
    public BlockSetting ContentType { get; set; } = BlockSettingFactory.CreateStringSetting("contentType", "application/octet-stream");
}
