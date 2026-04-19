namespace RuriLib.Models.Blocks.Custom.HttpRequest.Multipart;

public class FileHttpContent(string name, string fileName, string contentType) : MyHttpContent(name, contentType)
{
    public string FileName { get; set; } = fileName;
}
