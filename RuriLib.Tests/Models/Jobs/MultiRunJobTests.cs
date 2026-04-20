using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Jobs;
using RuriLib.Models.Jobs.StartConditions;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Models.Jobs;

public class MultiRunJobTests
{
    [Fact]
    public void Defaults_AreSafe()
    {
        var job = CreateJob();

        Assert.Equal(1, job.Bots);
        Assert.Null(job.Config);
        Assert.Null(job.DataPool);
        Assert.Empty(job.ProxySources);
        Assert.Empty(job.HitOutputs);
        Assert.Empty(job.CustomInputsAnswers);
        Assert.Empty(job.CurrentBotDatas);
        Assert.Null(job.Providers);
        Assert.False(job.ShouldUseProxies());
    }

    [Fact]
    public void ShouldUseProxies_UsesConfigSettingsWhenAvailable()
    {
        var job = CreateJob();
        job.Config = new Config
        {
            Id = "test",
            Settings = new ConfigSettings()
        };

        job.Config.Settings.ProxySettings.UseProxies = true;
        job.ProxyMode = JobProxyMode.Default;
        Assert.True(job.ShouldUseProxies());

        job.ProxyMode = JobProxyMode.Off;
        Assert.False(job.ShouldUseProxies());

        job.ProxyMode = JobProxyMode.On;
        Assert.True(job.ShouldUseProxies());
    }

    [Fact]
    public async Task Start_WithoutProviders_ThrowsInvalidOperationException()
    {
        var job = CreateJob();
        job.Config = new Config
        {
            Id = "test",
            Settings = new ConfigSettings()
        };
        job.DataPool = new TestDataPool();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => job.Start());

        Assert.Equal("The Providers cannot be null", exception.Message);
    }

    [Fact]
    public async Task FetchProxiesFromSources_BeforeStart_ThrowsInvalidOperationException()
    {
        var job = CreateJob();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => job.FetchProxiesFromSources());

        Assert.Equal("The job has not been initialized yet", exception.Message);
    }

    [Fact]
    public async Task Start_WithEmptyData_InitializesCurrentBotDatas()
    {
        var settings = CreateSettingsService();
        var job = new MultiRunJob(settings, CreatePluginRepository())
        {
            Bots = 3,
            Providers = new global::RuriLib.Models.Bots.Providers(settings),
            StartCondition = new ImmediateStartCondition(),
            Config = new Config
            {
                Id = "test",
                Mode = ConfigMode.Legacy,
                Settings = new ConfigSettings()
            },
            DataPool = new TestDataPool(["data"], settings.Environment.WordlistTypes[0].Name)
        };

        await job.Start();

        Assert.Equal(3, job.CurrentBotDatas.Length);
    }

    [Fact]
    public void ResetStats_AlsoResetsInvalidCount()
    {
        var job = CreateJob();

        typeof(MultiRunJob)
            .GetField("dataInvalid", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(job, 5);

        typeof(MultiRunJob)
            .GetMethod("ResetStats", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(job, null);

        Assert.Equal(0, job.DataInvalid);
    }

    private static MultiRunJob CreateJob()
        => new(CreateSettingsService(), CreatePluginRepository());

    private static global::RuriLib.Services.RuriLibSettingsService CreateSettingsService()
        => new(Path.Combine(Path.GetTempPath(), $"ob2-multirun-settings-{Guid.NewGuid():N}"));

    private static global::RuriLib.Services.PluginRepository CreatePluginRepository()
        => (global::RuriLib.Services.PluginRepository)RuntimeHelpers
            .GetUninitializedObject(typeof(global::RuriLib.Services.PluginRepository));

    private sealed class TestDataPool : DataPool
    {
        public TestDataPool()
        {
        }

        public TestDataPool(string[] data, string wordlistType)
        {
            DataList = data;
            Size = data.Length;
            WordlistType = wordlistType;
        }

        public override void Reload()
        {
        }
    }

    private sealed class ImmediateStartCondition : StartCondition
    {
        public override bool Verify(Job job) => true;
    }
}
