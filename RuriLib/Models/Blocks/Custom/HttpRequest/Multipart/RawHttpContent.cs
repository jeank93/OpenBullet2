namespace RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;

public class RawHttpContent(string name, byte[] data, string contentType) : MyHttpContent(name, contentType)
{
    public byte[] Data { get; set; } = data;
}
