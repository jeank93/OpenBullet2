using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Utils;

internal sealed class TestTcpServer : IAsyncDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Func<NetworkStream, CancellationToken, Task> handler;
    private readonly Task acceptTask;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private TestTcpServer(Func<NetworkStream, CancellationToken, Task> handler)
    {
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));

        listener.Start();
        acceptTask = Task.Run(AcceptClientAsync, CancellationToken.None);
    }

    public string Host => "127.0.0.1";

    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

    public static TestTcpServer CreateEchoServer()
        => new(async (stream, cancellationToken) =>
        {
            var bytes = await ReadOnceAsync(stream, cancellationToken);
            await stream.WriteAsync(bytes, cancellationToken);
        });

    public static TestTcpServer CreateResponseServer(string response, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(encoding);

        return new(async (stream, cancellationToken) =>
        {
            var bytes = encoding.GetBytes(response);
            await stream.WriteAsync(bytes, cancellationToken);
        });
    }

    public static TestTcpServer CreateResponseServer(byte[] response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new(async (stream, cancellationToken) =>
        {
            await stream.WriteAsync(response, cancellationToken);
        });
    }

    public static TestTcpServer CreateHttpServer(string responseBody, bool gzip = false)
    {
        ArgumentNullException.ThrowIfNull(responseBody);

        return new(async (stream, cancellationToken) =>
        {
            await ReadHeadersAsync(stream, cancellationToken);

            var payload = Encoding.UTF8.GetBytes(responseBody);
            if (gzip)
            {
                payload = Compress(payload);
            }

            var headers =
                $"HTTP/1.1 200 OK\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n";

            if (gzip)
            {
                headers += "Content-Encoding: gzip\r\n";
            }

            headers += "\r\n";

            await stream.WriteAsync(Encoding.UTF8.GetBytes(headers), cancellationToken);
            await stream.WriteAsync(payload, cancellationToken);
        });
    }

    public async ValueTask DisposeAsync()
    {
        cancellationTokenSource.Cancel();
        listener.Stop();

        try
        {
            await acceptTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
    }

    private async Task AcceptClientAsync()
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationTokenSource.Token,
            TestCancellationToken);

        using var client = await listener.AcceptTcpClientAsync(linkedCts.Token);
        await using var stream = client.GetStream();

        await handler(stream, linkedCts.Token);
    }

    private static async Task<byte[]> ReadOnceAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
        return new ArraySegment<byte>(buffer, 0, bytesRead).ToArray();
    }

    private static async Task ReadHeadersAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                throw new InvalidOperationException("The client closed the TCP stream before sending HTTP headers");
            }

            await ms.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

            if (Encoding.ASCII.GetString(ms.ToArray()).Contains("\r\n\r\n", StringComparison.Ordinal))
            {
                return;
            }
        }
    }

    private static byte[] Compress(byte[] payload)
    {
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
        {
            gzip.Write(payload, 0, payload.Length);
        }

        return ms.ToArray();
    }
}
