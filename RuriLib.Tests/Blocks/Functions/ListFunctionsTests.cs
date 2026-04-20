using RuriLib.Blocks.Functions.List;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils.Mockup;
using System.Collections.Generic;
using Xunit;

namespace RuriLib.Tests.Blocks.Functions;

public class ListFunctionsTests
{
    [Fact]
    public void ZipLists_FillEnabled_FillsShorterFirstList()
    {
        var data = NewBotData();

        var zipped = Methods.ZipLists(data, ["1"], ["a", "b"], fill: true, fillString: "NULL", format: "[0]-[1]");

        Assert.Equal(["1-a", "NULL-b"], zipped);
    }

    [Fact]
    public void ListToDictionary_IgnoresItemsWithoutSeparator()
    {
        var data = NewBotData();

        var dict = Methods.ListToDictionary(data, ["a:1", "invalid", "b:2"]);

        Assert.Equal("1", dict["a"]);
        Assert.Equal("2", dict["b"]);
        Assert.DoesNotContain("invalid", dict.Keys);
    }

    [Fact]
    public void AddAndRemoveFromList_NegativeIndex_UsesListEnd()
    {
        var data = NewBotData();
        var list = new List<string> { "a", "b" };

        Methods.AddToList(data, list, "c", -1);
        Methods.RemoveFromList(data, list, -1);

        Assert.Equal(["a", "b"], list);
    }

    [Fact]
    public void ListIndexOf_InexactCaseInsensitive_FindsMatch()
    {
        var data = NewBotData();

        var index = Methods.ListIndexOf(data, ["Alpha", "Beta"], "pha");

        Assert.Equal(0, index);
    }

    private static BotData NewBotData()
        => new(
            new global::RuriLib.Models.Bots.Providers(null!)
            {
                ProxySettings = new MockedProxySettingsProvider(),
                Security = new MockedSecurityProvider()
            },
            new ConfigSettings(),
            new BotLogger(),
            new DataLine("hello", new WordlistType()));
}
