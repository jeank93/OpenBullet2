using FluentFTP;
using FtpMethods = RuriLib.Blocks.Requests.Ftp.Methods;
using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Tests.Utils;
using RuriLib.Tests.Utils.Mockup;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Blocks.Requests;

[Collection(nameof(FtpServerCollection))]
public class FtpRequestBlocksTests
{
    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FtpConnect_ListItems_AndGetLog_Verify()
    {
        await TestFtpServer.ResetHomeDirectory();
        var homeDirectory = await TestFtpServer.GetHomeDirectory();
        Directory.CreateDirectory(Path.Combine(homeDirectory, "folder-a"));
        Directory.CreateDirectory(Path.Combine(homeDirectory, "folder-b"));
        await File.WriteAllTextAsync(Path.Combine(homeDirectory, "root.txt"), "root-content", TestCancellationToken);
        await File.WriteAllTextAsync(Path.Combine(homeDirectory, "folder-a", "nested.txt"), "nested-content", TestCancellationToken);

        var connection = await TestFtpServer.GetConnectionInfo();
        var data = NewBotData();

        await FtpMethods.FtpConnect(
            data,
            connection.Host,
            connection.Port,
            connection.Username,
            connection.Password,
            20000);

        var allItems = await FtpMethods.FtpListItems(data);
        var filesOnly = await FtpMethods.FtpListItems(data, global::RuriLib.Blocks.Requests.Ftp.FtpItemKind.File);
        var foldersOnly = await FtpMethods.FtpListItems(data, global::RuriLib.Blocks.Requests.Ftp.FtpItemKind.Folder);
        var recursiveFiles = await FtpMethods.FtpListItems(data, global::RuriLib.Blocks.Requests.Ftp.FtpItemKind.File, recursive: true);
        var ftpLog = FtpMethods.FtpGetLog(data);

        Assert.Contains(allItems, item => item.EndsWith("/root.txt", StringComparison.Ordinal));
        Assert.Contains(allItems, item => item.EndsWith("/folder-a", StringComparison.Ordinal));
        Assert.Contains(allItems, item => item.EndsWith("/folder-b", StringComparison.Ordinal));
        Assert.DoesNotContain(filesOnly, item => item.EndsWith("/folder-a", StringComparison.Ordinal));
        Assert.Contains(filesOnly, item => item.EndsWith("/root.txt", StringComparison.Ordinal));
        Assert.Contains(foldersOnly, item => item.EndsWith("/folder-a", StringComparison.Ordinal));
        Assert.DoesNotContain(foldersOnly, item => item.EndsWith("/root.txt", StringComparison.Ordinal));
        Assert.Contains(recursiveFiles, item => item.EndsWith("/folder-a/nested.txt", StringComparison.Ordinal));
        Assert.Contains("USER", ftpLog, StringComparison.Ordinal);
        Assert.True(ftpLog.Contains("LIST", StringComparison.Ordinal) || ftpLog.Contains("MLSD", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FtpDownloadFile_DownloadsRemoteContent()
    {
        await TestFtpServer.ResetHomeDirectory();
        var homeDirectory = await TestFtpServer.GetHomeDirectory();
        await File.WriteAllTextAsync(Path.Combine(homeDirectory, "remote.txt"), "downloaded-content", TestCancellationToken);

        var connection = await TestFtpServer.GetConnectionInfo();
        var data = NewBotData();
        var localFile = Path.Combine(Path.GetTempPath(), $"ob2-download-{Guid.NewGuid():N}.txt");

        try
        {
            await FtpMethods.FtpConnect(
                data,
                connection.Host,
                connection.Port,
                connection.Username,
                connection.Password,
                20000);

            await FtpMethods.FtpDownloadFile(data, "/remote.txt", localFile);

            Assert.Equal("downloaded-content", await File.ReadAllTextAsync(localFile, TestCancellationToken));
        }
        finally
        {
            await FtpMethods.FtpDisconnect(data);
            if (File.Exists(localFile))
            {
                File.Delete(localFile);
            }
        }
    }

    [Fact]
    public async Task FtpDownloadFolder_WithSkip_KeepsExistingLocalFilesAndDownloadsMissingOnes()
    {
        await TestFtpServer.ResetHomeDirectory();
        var homeDirectory = await TestFtpServer.GetHomeDirectory();
        Directory.CreateDirectory(Path.Combine(homeDirectory, "remote-folder"));
        await File.WriteAllTextAsync(Path.Combine(homeDirectory, "remote-folder", "keep.txt"), "remote-version", TestCancellationToken);
        await File.WriteAllTextAsync(Path.Combine(homeDirectory, "remote-folder", "new.txt"), "new-file", TestCancellationToken);

        var connection = await TestFtpServer.GetConnectionInfo();
        var data = NewBotData();
        var localDirectory = Path.Combine(Path.GetTempPath(), $"ob2-folder-{Guid.NewGuid():N}");

        Directory.CreateDirectory(localDirectory);
        await File.WriteAllTextAsync(Path.Combine(localDirectory, "keep.txt"), "local-version", TestCancellationToken);

        try
        {
            await FtpMethods.FtpConnect(
                data,
                connection.Host,
                connection.Port,
                connection.Username,
                connection.Password,
                20000);

            await FtpMethods.FtpDownloadFolder(
                data,
                "/remote-folder",
                localDirectory,
                FtpLocalExists.Skip);

            Assert.Equal("local-version", await File.ReadAllTextAsync(Path.Combine(localDirectory, "keep.txt"), TestCancellationToken));
            Assert.Equal("new-file", await File.ReadAllTextAsync(Path.Combine(localDirectory, "new.txt"), TestCancellationToken));
        }
        finally
        {
            await FtpMethods.FtpDisconnect(data);
            if (Directory.Exists(localDirectory))
            {
                Directory.Delete(localDirectory, true);
            }
        }
    }

    [Fact]
    public async Task FtpUploadFile_OverwritesRemoteFile_AndDisconnectsClient()
    {
        await TestFtpServer.ResetHomeDirectory();
        var homeDirectory = await TestFtpServer.GetHomeDirectory();
        await File.WriteAllTextAsync(Path.Combine(homeDirectory, "uploaded.txt"), "old-content", TestCancellationToken);

        var connection = await TestFtpServer.GetConnectionInfo();
        var data = NewBotData();
        var localFile = Path.Combine(Path.GetTempPath(), $"ob2-upload-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(localFile, "new-content", TestCancellationToken);

        try
        {
            await FtpMethods.FtpConnect(
                data,
                connection.Host,
                connection.Port,
                connection.Username,
                connection.Password,
                20000);

            await FtpMethods.FtpUploadFile(data, "/uploaded.txt", localFile);

            Assert.Equal("new-content", await File.ReadAllTextAsync(Path.Combine(homeDirectory, "uploaded.txt"), TestCancellationToken));

            await FtpMethods.FtpDisconnect(data);

            var client = data.TryGetObject<AsyncFtpClient>("ftpClient");
            Assert.NotNull(client);
            Assert.False(client!.IsConnected);
        }
        finally
        {
            if (File.Exists(localFile))
            {
                File.Delete(localFile);
            }
        }
    }

    [Fact]
    public async Task FtpDisconnect_WithoutClient_Throws()
    {
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<BlockExecutionException>(() =>
            FtpMethods.FtpDisconnect(data));

        Assert.Equal("Connect to a server first!", ex.Message);
    }

    [Fact]
    public async Task FtpConnect_WithWrongPassword_Throws()
    {
        var connection = await TestFtpServer.GetConnectionInfo();
        var data = NewBotData();

        var ex = await Assert.ThrowsAsync<FluentFTP.Exceptions.FtpAuthenticationException>(() =>
            FtpMethods.FtpConnect(
                data,
                connection.Host,
                connection.Port,
                connection.Username,
                "wrong-password",
                20000));
        Assert.Contains("530", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Login authentication failed", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FtpConnect_ToUnusedPort_Throws()
    {
        var data = NewBotData();
        var unusedPort = FindUnusedTcpPort();

        var ex = await Assert.ThrowsAsync<TimeoutException>(() =>
            FtpMethods.FtpConnect(
                data,
                "127.0.0.1",
                unusedPort,
                "user",
                "password",
                1000));
        Assert.Equal("Timed out trying to connect!", ex.Message);
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
            new DataLine("ftp-test", new WordlistType()));

    private static int FindUnusedTcpPort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }
}

[CollectionDefinition(nameof(FtpServerCollection), DisableParallelization = true)]
public class FtpServerCollection;
