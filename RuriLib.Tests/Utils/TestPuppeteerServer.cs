using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Utils;

internal static class TestPuppeteerServer
{
    private const ushort DevToolsPort = 9222;
    private const string ContainerImage = "chromedp/headless-shell:stable";
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private static readonly Dictionary<string, BrowserState> States = [];

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    public static async Task<Uri> GetBrowserUrl(INetwork network, string pool = "default", params string[] commandLineArgs)
    {
        if (!States.TryGetValue(pool, out var state))
        {
            state = new(commandLineArgs);
            States[pool] = state;
        }

        await EnsureInitialized(network, state);
        if (state.SkipReason is not null)
        {
            Assert.Skip(state.SkipReason);
        }

        return state.BrowserUrl!;
    }

    private static async Task EnsureInitialized(INetwork network, BrowserState state)
    {
        if (state.BrowserUrl is not null || state.SkipReason is not null)
        {
            return;
        }

        await SyncLock.WaitAsync(TestCancellationToken);
        try
        {
            if (state.BrowserUrl is not null || state.SkipReason is not null)
            {
                return;
            }

            try
            {
                var builder = new ContainerBuilder(ContainerImage)
                    .WithNetwork(network)
                    .WithPortBinding(DevToolsPort, true)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(DevToolsPort));

                if (state.CommandLineArgs.Length > 0)
                {
                    builder = builder.WithCommand(state.CommandLineArgs);
                }

                state.Container = builder.Build();

                await state.Container.StartAsync(TestCancellationToken);

                var candidateBrowserUrl = new Uri($"http://127.0.0.1:{state.Container.GetMappedPublicPort(DevToolsPort)}");
                await WaitUntilReady(candidateBrowserUrl);

                state.BrowserUrl = candidateBrowserUrl;
                AppDomain.CurrentDomain.ProcessExit += state.DisposeContainerOnProcessExit;
            }
            catch (Exception ex)
            {
                await state.DisposeContainer();
                state.SkipReason = $"Docker is unavailable for {ContainerImage}: {ex.GetType().Name}: {ex.Message}";
            }
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private static async Task WaitUntilReady(Uri candidateBrowserUrl)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        var versionUrl = new Uri(candidateBrowserUrl, "/json/version");

        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(versionUrl, TestCancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1), TestCancellationToken);
        }

        throw new TimeoutException("Timed out waiting for the Puppeteer container");
    }

    private sealed class BrowserState(string[] commandLineArgs)
    {
        public string[] CommandLineArgs { get; } = commandLineArgs;
        public IContainer? Container { get; set; }
        public Uri? BrowserUrl { get; set; }
        public string? SkipReason { get; set; }

        public void DisposeContainerOnProcessExit(object? sender, EventArgs e)
            => DisposeContainer().GetAwaiter().GetResult();

        public async Task DisposeContainer()
        {
            if (Container is null)
            {
                return;
            }

            try
            {
                await Container.DisposeAsync();
            }
            finally
            {
                AppDomain.CurrentDomain.ProcessExit -= DisposeContainerOnProcessExit;
                Container = null;
            }
        }
    }
}
