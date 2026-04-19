namespace RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;

public abstract class MyHttpContent(string name, string contentType)
{
    public string Name { get; set; } = name;
    public string ContentType { get; set; } = contentType;
}
