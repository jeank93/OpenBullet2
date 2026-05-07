using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Jobs;
using RuriLib.Models.Jobs.StartConditions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Models.Jobs;

public class MultiRunJobTests
{
    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

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

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => job.Start(TestCancellationToken));

        Assert.Equal("The Providers cannot be null", exception.Message);
    }

    [Fact]
    public async Task FetchProxiesFromSources_BeforeStart_ThrowsInvalidOperationException()
    {
        var job = CreateJob();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => job.FetchProxiesFromSources(TestCancellationToken));

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

        await job.Start(TestCancellationToken);

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

    [Theory]
    [InlineData(0, 2, 5, 2)]
    [InlineData(2, 1, 5, 3)]
    [InlineData(2, 3, 5, 0)]
    [InlineData(0, 5, 5, 0)]
    public void GetNextSkip_NormalizesCompletedRuns(int currentSkip, int processed, int total, int expected)
    {
        var nextSkip = MultiRunJobCheckpoint.GetNextSkip(currentSkip, processed, total);

        Assert.Equal(expected, nextSkip);
    }

    [Fact]
    public async Task Start_AfterCompletedRun_RestartsFromBeginning()
    {
        var settings = CreateSettingsService();
        var job = new MultiRunJob(settings, CreatePluginRepository())
        {
            Bots = 1,
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

        await job.Start(TestCancellationToken);
        await WaitUntilIdleAsync(job);
        Assert.Equal(0, job.Skip);

        var exception = await Record.ExceptionAsync(() => job.Start(TestCancellationToken));
        await WaitUntilIdleAsync(job);

        Assert.Null(exception);
        Assert.Equal(0, job.Skip);
    }

    [Fact]
    public async Task GetHitsSnapshot_DuringConcurrentWrites_DoesNotThrow()
    {
        var job = CreateJob();
        var hits = job.Hits;
        var hitsLock = typeof(MultiRunJob)
            .GetField("hitsLock", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(job)!;
        using var cts = new CancellationTokenSource();

        var writer = Task.Run(async () =>
        {
            var index = 0;

            while (!cts.Token.IsCancellationRequested)
            {
                lock (hitsLock)
                {
                    hits.Add(CreateHit(index++));
                }

                await Task.Yield();
            }
        }, TestCancellationToken);

        for (var i = 0; i < 200; i++)
        {
            var exception = Record.Exception(() => job.GetHitsSnapshot());
            Assert.Null(exception);
            await Task.Yield();
        }

        await cts.CancelAsync();
        await writer.WaitAsync(TestCancellationToken);
    }

    [Fact]
    public void FindHit_ReturnsMatchingHit()
    {
        var job = CreateJob();
        var hit = CreateHit(1);
        job.Hits.Add(hit);

        var found = job.FindHit(hit.Id);

        Assert.Same(hit, found);
    }

    private static MultiRunJob CreateJob()
        => new(CreateSettingsService(), CreatePluginRepository());

    private static global::RuriLib.Services.RuriLibSettingsService CreateSettingsService()
        => new(Path.Combine(Path.GetTempPath(), $"ob2-multirun-settings-{Guid.NewGuid():N}"));

    private static global::RuriLib.Services.PluginRepository CreatePluginRepository()
        => (global::RuriLib.Services.PluginRepository)RuntimeHelpers
            .GetUninitializedObject(typeof(global::RuriLib.Services.PluginRepository));

    private static async Task WaitUntilIdleAsync(MultiRunJob job)
    {
        for (var i = 0; i < 50 && job.Status != JobStatus.Idle; i++)
        {
            await Task.Delay(20, TestCancellationToken);
        }

        Assert.Equal(JobStatus.Idle, job.Status);
    }

    private static global::RuriLib.Models.Hits.Hit CreateHit(int index)
    {
        const string wordlistTypeName = "default";

        return new global::RuriLib.Models.Hits.Hit
        {
            Data = new DataLine(
                $"user{index}:pass{index}",
                new global::RuriLib.Models.Environment.WordlistType { Name = wordlistTypeName }),
            CapturedData = new Dictionary<string, object> { ["token"] = $"abc{index}" },
            Date = DateTime.UtcNow,
            Type = "SUCCESS",
            Config = new Config
            {
                Id = $"cfg-{index}",
                Metadata = new ConfigMetadata { Name = "Config", Category = "Cat" }
            },
            DataPool = new TestDataPool([$"user{index}:pass{index}"], wordlistTypeName)
        };
    }

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
