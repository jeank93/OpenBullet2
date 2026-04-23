using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentFTP;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Utils;

internal static class TestFtpServer
{
    private const ushort ControlPort = 21;
    private const string ContainerImage = "stilliard/pure-ftpd:trixie-latest";
    private const string Username = "ob2-user";
    private const string Password = "ob2-password";
    private const string ContainerHomeDirectory = "/home/ftpusers/ob2-user";
    private const int PassivePortCount = 10;
    private static readonly SemaphoreSlim SyncLock = new(1, 1);
    private static IContainer? container;
    private static string? skipReason;
    private static string? homeDirectory;
    private static FtpServerConnectionInfo? connectionInfo;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    public static async Task<FtpServerConnectionInfo> GetConnectionInfo()
    {
        await EnsureInitialized();
        if (skipReason is not null)
        {
            Assert.Skip(skipReason);
        }

        return connectionInfo!;
    }

    public static async Task ResetHomeDirectory()
    {
        _ = await GetConnectionInfo();

        foreach (var entry in Directory.EnumerateFileSystemEntries(homeDirectory!))
        {
            var attributes = File.GetAttributes(entry);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                Directory.Delete(entry, true);
            }
            else
            {
                File.Delete(entry);
            }
        }
    }

    public static async Task<string> GetHomeDirectory()
    {
        _ = await GetConnectionInfo();
        return homeDirectory!;
    }

    private static async Task EnsureInitialized()
    {
        if (connectionInfo is not null || skipReason is not null)
        {
            return;
        }

        await SyncLock.WaitAsync(TestCancellationToken);
        try
        {
            if (connectionInfo is not null || skipReason is not null)
            {
                return;
            }

            var passivePortStart = FindFreePortRangeStart(30000, 40000, PassivePortCount);
            var passivePortEnd = passivePortStart + PassivePortCount - 1;
            homeDirectory = Path.Combine(Path.GetTempPath(), $"ob2-ftp-{Guid.NewGuid():N}");
            Directory.CreateDirectory(homeDirectory);

            try
            {
                var builder = new ContainerBuilder(ContainerImage)
                    .WithPortBinding(ControlPort, true)
                    .WithBindMount(homeDirectory, ContainerHomeDirectory)
                    .WithEnvironment("PUBLICHOST", "127.0.0.1")
                    .WithEnvironment("FTP_USER_NAME", Username)
                    .WithEnvironment("FTP_USER_PASS", Password)
                    .WithEnvironment("FTP_USER_HOME", ContainerHomeDirectory)
                    .WithEnvironment("FTP_PASSIVE_PORTS", $"{passivePortStart}:{passivePortEnd}")
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(ControlPort));

                for (var port = passivePortStart; port <= passivePortEnd; port++)
                {
                    builder = builder.WithPortBinding((ushort)port, (ushort)port);
                }

                container = builder.Build();

                await container.StartAsync(TestCancellationToken);

                connectionInfo = new FtpServerConnectionInfo(
                    "127.0.0.1",
                    container.GetMappedPublicPort(ControlPort),
                    Username,
                    Password);

                await WaitUntilReady(connectionInfo);
                AppDomain.CurrentDomain.ProcessExit += DisposeContainerOnProcessExit;
            }
            catch (Exception ex)
            {
                await DisposeContainer();
                skipReason = $"Docker is unavailable for {ContainerImage}: {ex.GetType().Name}: {ex.Message}";
            }
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private static async Task WaitUntilReady(FtpServerConnectionInfo info)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            await using var client = new AsyncFtpClient(
                info.Host,
                new NetworkCredential(info.Username, info.Password),
                info.Port,
                new FtpConfig
                {
                    ConnectTimeout = 15000,
                    ReadTimeout = 15000,
                    DataConnectionConnectTimeout = 15000,
                    DataConnectionReadTimeout = 15000
                });

            try
            {
                await client.AutoConnect(TestCancellationToken).ConfigureAwait(false);
                if (client.IsConnected)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1), TestCancellationToken);
        }

        throw new TimeoutException("Timed out waiting for the local FTP container");
    }

    private static int FindFreePortRangeStart(int startInclusive, int endExclusive, int count)
    {
        for (var start = startInclusive; start <= endExclusive - count; start++)
        {
            if (Enumerable.Range(start, count).All(IsPortAvailable))
            {
                return start;
            }
        }

        throw new InvalidOperationException($"Could not find {count} contiguous free ports in the range {startInclusive}-{endExclusive - 1}");
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static void DisposeContainerOnProcessExit(object? sender, EventArgs e)
        => DisposeContainer().GetAwaiter().GetResult();

    private static async Task DisposeContainer()
    {
        if (container is not null)
        {
            try
            {
                await container.DisposeAsync();
            }
            finally
            {
                AppDomain.CurrentDomain.ProcessExit -= DisposeContainerOnProcessExit;
                container = null;
            }
        }

        if (homeDirectory is not null && Directory.Exists(homeDirectory))
        {
            Directory.Delete(homeDirectory, true);
            homeDirectory = null;
        }
    }
}

internal sealed record FtpServerConnectionInfo(string Host, ushort Port, string Username, string Password);
