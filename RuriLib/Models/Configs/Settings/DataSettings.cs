using RuriLib.Models.Data.Resources.Options;
using RuriLib.Models.Data.Rules;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RuriLib.Models.Configs.Settings;

public class DataSettings
{
    public string[] AllowedWordlistTypes { get; set; } = ["Default"];
    public bool UrlEncodeDataAfterSlicing { get; set; }
    public List<DataRule> DataRules { get; set; } = [];
    public List<ConfigResourceOptions> Resources { get; set; } = [];

    [JsonIgnore]
    public string AllowedWordlistTypesString => string.Join(", ", AllowedWordlistTypes);
}
