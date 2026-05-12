using RuriLib.Models.Jobs;
using RuriLib.Models.Proxies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Models.Jobs;

public class ProxyCheckJobTests
{
    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public void Defaults_AreSafe()
    {
        var job = CreateJob();

        Assert.Equal(1, job.Bots);
        Assert.Equal("https://google.com", job.Url);
        Assert.Equal("title>Google", job.SuccessKey);
        Assert.Null(job.Proxies);
        Assert.Null(job.ProxyOutput);
        Assert.Null(job.GeoProvider);
        Assert.Equal(TimeSpan.FromSeconds(10), job.Timeout);
    }

    [Fact]
    public async Task Start_WithoutProxies_Throws()
    {
        var job = CreateJob();
        job.ProxyOutput = new NullProxyCheckOutput();

        var exception = await Assert.ThrowsAsync<NullReferenceException>(() => job.Start(TestCancellationToken));

        Assert.Equal("The proxy list cannot be null", exception.Message);
    }

    [Fact]
    public async Task Start_WithoutOutput_Throws()
    {
        var job = CreateJob();
        job.Proxies = new List<Proxy>();

        var exception = await Assert.ThrowsAsync<NullReferenceException>(() => job.Start(TestCancellationToken));

        Assert.Equal("The proxy check output cannot be null", exception.Message);
    }

    [Fact]
    public async Task ChangeBots_WithoutParallelizer_StillUpdatesBots()
    {
        var job = CreateJob();

        await job.ChangeBots(5);

        Assert.Equal(5, job.Bots);
    }

    private static ProxyCheckJob CreateJob()
        => new(CreateSettingsService(), CreatePluginRepository());

    private static global::RuriLib.Services.RuriLibSettingsService CreateSettingsService()
        => new(Path.Combine(Path.GetTempPath(), $"ob2-proxycheck-settings-{Guid.NewGuid():N}"));

    private static global::RuriLib.Services.PluginRepository CreatePluginRepository()
        => new(Path.Combine(Path.GetTempPath(), $"ob2-proxycheck-plugins-{Guid.NewGuid():N}"));

    private sealed class NullProxyCheckOutput : IProxyCheckOutput
    {
        public Task StoreAsync(Proxy proxy) => Task.CompletedTask;
    }
}
