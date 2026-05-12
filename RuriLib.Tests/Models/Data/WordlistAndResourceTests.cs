using RuriLib.Models.Data;
using RuriLib.Models.Data.DataPools;
using RuriLib.Models.Data.Resources;
using RuriLib.Models.Data.Resources.Options;
using RuriLib.Models.Environment;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RuriLib.Tests.Models.Data;

public class WordlistAndResourceTests
{
    [Fact]
    public void Wordlist_CountLinesFalse_AllowsInMemoryWordlist()
    {
        var type = new WordlistType { Name = "Default" };

        var wordlist = new Wordlist("test", null, type, null, countLines: false);

        Assert.Null(wordlist.Path);
        Assert.Equal(string.Empty, wordlist.Purpose);
        Assert.Equal(0, wordlist.Total);
    }

    [Fact]
    public void WordlistDataPool_WordlistWithoutPath_ThrowsExplicitly()
    {
        var wordlist = new Wordlist("test", null, new WordlistType { Name = "Default" }, null, countLines: false);

        var exception = Assert.Throws<ArgumentException>(() => new WordlistDataPool(wordlist));

        Assert.Contains("reside on disk", exception.Message);
    }

    [Fact]
    public void FileDataPool_NullFileName_Throws() => Assert.Throws<ArgumentNullException>(() => new FileDataPool(null!));

    [Fact]
    public void LinesFromFileResource_LoopsAroundAfterEnd()
    {
        var fileName = Path.GetTempFileName();

        try
        {
            File.WriteAllText(fileName, $"one{Environment.NewLine}two{Environment.NewLine}");
            using var resource = new LinesFromFileResource(new LinesFromFileResourceOptions
            {
                Location = fileName,
                LoopsAround = true
            });

            Assert.Equal(["one", "two", "one"], resource.Take(3));
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    [Fact]
    public void RandomLinesFromFileResource_Unique_ExhaustsSource()
    {
        var fileName = Path.GetTempFileName();

        try
        {
            File.WriteAllText(fileName, $"one{Environment.NewLine}two{Environment.NewLine}");
            var resource = new RandomLinesFromFileResource(new RandomLinesFromFileResourceOptions
            {
                Location = fileName,
                Unique = true
            });

            var taken = resource.Take(2);

            Assert.Equal(2, taken.Distinct().Count());
            Assert.Throws<Exception>(() => resource.TakeOne());
        }
        finally
        {
            File.Delete(fileName);
        }
    }
}
