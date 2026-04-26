using IronPython.Hosting;
using Jint;
using Microsoft.Scripting.Hosting;
using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils.Mockup;
using System;
using System.IO;
using Xunit;
using BotProviders = RuriLib.Models.Bots.Providers;
using InteropMethods = RuriLib.Blocks.Interop.Methods;

namespace RuriLib.Tests.Blocks.Interop;

public class InteropBlocksTests
{
    [Fact]
    public void ShellCommand_DotnetVersion_ReturnsOutput()
    {
        var data = NewBotData();

        var output = InteropMethods.ShellCommand(data, "dotnet", "--version");

        Assert.False(string.IsNullOrWhiteSpace(output));
        Assert.Contains(".", output);
    }

    [Fact]
    public void InvokeJint_ScriptFile_ExecutesAndReturnsEngine()
    {
        var data = NewBotData();
        var engine = new Engine();
        var tempFile = Path.Combine(Path.GetTempPath(), $"{nameof(InteropBlocksTests)}-{Guid.NewGuid():N}.js");

        try
        {
            File.WriteAllText(tempFile, "var result = 2 + 3;");

            var resultEngine = InteropMethods.InvokeJint(data, engine, tempFile);

            Assert.Equal(5, resultEngine.GetValue("result").AsNumber());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetIronPyScope_WithoutEngine_Throws()
    {
        var data = NewBotData();

        Assert.Throws<BlockExecutionException>(() => InteropMethods.GetIronPyScope(data));
    }

    [Fact]
    public void GetIronPyScope_WithEngine_ReturnsScope()
    {
        var data = NewBotData();
        var engine = Python.CreateEngine();
        data.SetObject("ironPyEngine", engine);

        var scope = InteropMethods.GetIronPyScope(data);

        Assert.IsType<ScriptScope>(scope);
    }

    private static BotData NewBotData()
        => new(
            new BotProviders(null!)
            {
                ProxySettings = new MockedProxySettingsProvider(),
                Security = new MockedSecurityProvider()
            },
            new ConfigSettings(),
            new BotLogger(),
            new DataLine("hello", new WordlistType()));
}
