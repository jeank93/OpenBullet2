using OpenBullet2.Core.Helpers;
using System.Net;

namespace OpenBullet2.Web.Tests.Unit.Core;

public class FirewallTests
{
    [Fact]
    public async Task CheckIpValidityAsync_ExactIpv4Match_ReturnsTrue()
    {
        var isValid = await Firewall.CheckIpValidityAsync(
            IPAddress.Parse("192.168.1.10"),
            ["192.168.1.10"]);

        Assert.True(isValid);
    }

    [Fact]
    public async Task CheckIpValidityAsync_ExactIpv6Match_ReturnsTrue()
    {
        var isValid = await Firewall.CheckIpValidityAsync(
            IPAddress.Parse("2001:db8::1"),
            ["2001:db8::1"]);

        Assert.True(isValid);
    }

    [Fact]
    public async Task CheckIpValidityAsync_Ipv4SubnetMatch_ReturnsTrue()
    {
        var isValid = await Firewall.CheckIpValidityAsync(
            IPAddress.Parse("192.168.1.55"),
            ["192.168.1.0/24"]);

        Assert.True(isValid);
    }

    [Fact]
    public async Task CheckIpValidityAsync_InvalidEntriesAreIgnored_ReturnsFalse()
    {
        var isValid = await Firewall.CheckIpValidityAsync(
            IPAddress.Parse("10.0.0.1"),
            ["not-a-host", "300.300.300.300", "192.168.1.0/not-a-mask"]);

        Assert.False(isValid);
    }

    [Fact]
    public async Task CheckIpValidityAsync_DynamicDnsMatch_ReturnsTrue()
    {
        var localhost = await Dns.GetHostEntryAsync("localhost", TestContext.Current.CancellationToken);
        var ip = localhost.AddressList.First(a =>
            a.AddressFamily is System.Net.Sockets.AddressFamily.InterNetwork
            or System.Net.Sockets.AddressFamily.InterNetworkV6);

        var isValid = await Firewall.CheckIpValidityAsync(ip, ["localhost"]);

        Assert.True(isValid);
    }
}
