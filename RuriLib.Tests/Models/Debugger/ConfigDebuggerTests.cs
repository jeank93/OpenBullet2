using RuriLib.Logging;
using RuriLib.Models.Configs;
using RuriLib.Models.Debugger;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Models.Debugger;

public class ConfigDebuggerTests
{
    [Fact]
    public void Constructor_UsesSafeDefaults()
    {
        var config = new Config
        {
            Id = "test"
        };

        using var debugger = new ConfigDebugger(config);

        Assert.Same(config, debugger.Config);
        Assert.NotNull(debugger.Options);
        Assert.NotNull(debugger.Logger);
        Assert.Equal(ConfigDebuggerStatus.Idle, debugger.Status);
    }

    [Fact]
    public void TryTakeStep_WhenNotRunning_ReturnsFalse()
    {
        using var debugger = new ConfigDebugger(new Config { Id = "test" });

        Assert.False(debugger.TryTakeStep());
    }

    [Fact]
    public void Stop_WhenNotRunning_DoesNotThrow()
    {
        using var debugger = new ConfigDebugger(new Config { Id = "test" });

        var exception = Record.Exception(debugger.Stop);

        Assert.Null(exception);
    }

    [Fact]
    public async Task Run_WithoutSettingsService_Throws()
    {
        using var debugger = CreateDebugger();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => debugger.Run());

        Assert.Contains(nameof(ConfigDebugger.RuriLibSettings), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WithoutRngProvider_Throws()
    {
        using var debugger = CreateDebugger();
        debugger.RuriLibSettings = CreateSettingsService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => debugger.Run());

        Assert.Contains(nameof(ConfigDebugger.RNGProvider), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WithoutPluginRepository_Throws()
    {
        using var debugger = CreateDebugger();
        debugger.RuriLibSettings = CreateSettingsService();
        debugger.RNGProvider = new global::RuriLib.Providers.RandomNumbers.DefaultRNGProvider();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => debugger.Run());

        Assert.Contains(nameof(ConfigDebugger.PluginRepo), exception.Message, StringComparison.Ordinal);
    }

    private static ConfigDebugger CreateDebugger()
        => new(new Config
        {
            Id = "test"
        }, new DebuggerOptions(), new BotLogger());

    private static global::RuriLib.Services.RuriLibSettingsService CreateSettingsService()
        => new(Path.Combine(Path.GetTempPath(), $"ob2-config-debugger-tests-{Guid.NewGuid():N}"));
}
