namespace RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;

public class StringHttpContent(string name, string data, string contentType) : MyHttpContent(name, contentType)
{
    public string Data { get; set; } = data;
}
