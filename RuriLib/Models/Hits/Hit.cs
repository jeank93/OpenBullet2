using RuriLib.Logging;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Models.Proxies;
using RuriLib.Models.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuriLib.Models.Hits;

public class Hit
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public DataLine Data { get; set; } = new(string.Empty, new WordlistType());
    public string DataString => Data.Data;
    public Dictionary<string, object> CapturedData { get; set; } = [];
    public string CapturedDataString => ConvertCapturedData();
    public Proxy? Proxy { get; set; }
    public string ProxyString => Proxy?.ToString() ?? string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public Config Config { get; set; } = new() { Id = string.Empty };
    public DataPool? DataPool { get; set; }
    public IBotLogger? BotLogger { get; set; }
    public int OwnerId { get; set; } = -1;

    public override string ToString() => $"{DataString} | {CapturedDataString}";

    private string ConvertCapturedData()
    {
        var variables = new List<Variable>();

        foreach (var data in CapturedData)
        {
            try
            {
                var variable = VariableFactory.FromObject(data.Value);
                variable.Name = data.Key;
                variables.Add(variable);
            }
            catch
            {
                // If the variable is null, the snippet above will throw an exception, so just
                // add a dummy string variable with the literal value "null".
                variables.Add(new StringVariable("null") { Name = data.Key });
            }
        }

        return string.Join(" | ", variables.Select(v => $"{v.Name} = {v.AsString()}"));
    }
}
