using RuriLib.Logging;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Models.Hits;
using RuriLib.Models.Hits.HitOutputs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Models.Hits;

public class HitModelsTests
{
    [Fact]
    public void Hit_Defaults_AreSafe()
    {
        var hit = new Hit();

        Assert.Equal(string.Empty, hit.DataString);
        Assert.Equal(string.Empty, hit.ProxyString);
        Assert.Empty(hit.CapturedData);
        Assert.Equal(-1, hit.OwnerId);
        Assert.Null(hit.DataPool);
        Assert.Null(hit.BotLogger);
    }

    [Fact]
    public void Hit_CapturedDataString_UsesNullFallback()
    {
        var hit = new Hit
        {
            CapturedData = new Dictionary<string, object>
            {
                ["TOKEN"] = null!
            }
        };

        Assert.Equal("TOKEN = null", hit.CapturedDataString);
    }

    [Fact]
    public async Task FileSystemHitOutput_Store_WritesUsingEnvironmentNewLine()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), $"ob2-hit-output-{Guid.NewGuid():N}");
        var output = new FileSystemHitOutput(baseDir);
        var hit = new Hit
        {
            Type = "SUCCESS",
            Config = new Config
            {
                Id = "test",
                Metadata = new ConfigMetadata { Name = "Config" }
            },
            Data = new DataLine("user:pass", new WordlistType())
        };

        try
        {
            await output.Store(hit);

            var filePath = Path.Combine(baseDir, "Config", "SUCCESS.txt");
            var content = File.ReadAllText(filePath);

            Assert.EndsWith($"{System.Environment.NewLine}", content);
            Assert.Contains("user:pass", content);
        }
        finally
        {
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, true);
            }
        }
    }
}
