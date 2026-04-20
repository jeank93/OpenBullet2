using Newtonsoft.Json;

namespace RuriLib.Models.Hits.HitOutputs;

public class CustomWebhookData
{
    [JsonProperty("data")]
    public string Data { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("capturedData")]
    public string CapturedData { get; set; } = string.Empty;

    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }

    [JsonProperty("configName")]
    public string ConfigName { get; set; } = string.Empty;

    [JsonProperty("configAuthor")]
    public string ConfigAuthor { get; set; } = string.Empty;

    [JsonProperty("user")]
    public string User { get; set; } = string.Empty;
}
