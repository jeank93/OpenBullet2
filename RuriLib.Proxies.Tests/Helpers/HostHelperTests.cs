using RuriLib.Proxies.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Proxies.Tests.Helpers;

public class HostHelperTests
{
    [Fact]
    public async Task GetHostAddressesAsync_Cancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await HostHelper.GetHostAddressesAsync("localhost", cts.Token));
    }

    [Fact]
    public async Task GetIpAddressBytesAsync_Cancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await HostHelper.GetIpAddressBytesAsync("localhost", cancellationToken: cts.Token));
    }
}
