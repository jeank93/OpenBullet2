using System.Linq;
using RuriLib.Helpers.CSharp;
using RuriLib.Models.Configs.Settings;
using Xunit;

namespace RuriLib.Tests.Helpers.CSharp;

public class ScriptBuilderTests
{
    [Fact]
    public void Build_CustomUsingDirective_AddsParsedImport()
    {
        var settings = new ScriptSettings
        {
            CustomUsings =
            [
                "using System.Text;",
                " System.Globalization "
            ]
        };

        var script = new ScriptBuilder().Build("return 1;", settings, null!);

        Assert.Contains("System.Text", script.Options.Imports);
        Assert.Contains("System.Globalization", script.Options.Imports);
    }

    [Fact]
    public void GetUsings_DoesNotContainDuplicates()
    {
        var usings = ScriptBuilder.GetUsings().ToList();

        Assert.Equal(usings.Count, usings.Distinct().Count());
    }
}
