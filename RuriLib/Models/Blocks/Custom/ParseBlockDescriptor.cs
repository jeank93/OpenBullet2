using RuriLib.Models.Blocks.Parameters;
using RuriLib.Models.Blocks.Settings;

namespace RuriLib.Models.Blocks.Custom;

/// <summary>
/// Descriptor for the parse block.
/// </summary>
public class ParseBlockDescriptor : BlockDescriptor
{
    /// <summary>
    /// Initializes a new <see cref="ParseBlockDescriptor"/>.
    /// </summary>
    public ParseBlockDescriptor()
    {
        Id = "Parse";
        Name = Id;
        Description = "Parses text from a string";
        Category = new()
        {
            Name = "Parsing",
            BackgroundColor = "#ffd700",
            ForegroundColor = "#000",
            Path = "RuriLib.Blocks.Parsing",
            Namespace = "RuriLib.Blocks.Parsing.Methods",
            Description = "Blocks for extracting data from strings"
        };

        Parameters = new()
        {
            ["input"] = new StringParameter("input", "data.SOURCE", SettingInputMode.Variable),
            ["prefix"] = new StringParameter("prefix"),
            ["suffix"] = new StringParameter("suffix"),
            ["urlEncodeOutput"] = new BoolParameter("urlEncodeOutput", false),
            ["leftDelim"] = new StringParameter("leftDelim"),
            ["rightDelim"] = new StringParameter("rightDelim"),
            ["caseSensitive"] = new BoolParameter("caseSensitive", true),
            ["cssSelector"] = new StringParameter("cssSelector"),
            ["attributeName"] = new StringParameter("attributeName", "innerText"),
            ["xPath"] = new StringParameter("xPath"),
            ["jToken"] = new StringParameter("jToken"),
            ["pattern"] = new StringParameter("pattern"),
            ["outputFormat"] = new StringParameter("outputFormat"),
            ["multiLine"] = new BoolParameter("multiLine", false)
        };
    }
}
