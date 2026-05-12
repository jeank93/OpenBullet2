using Microsoft.Extensions.DependencyInjection;
using OpenBullet2.Core.Services;
using OpenBullet2.Native.Services;
using OpenBullet2.Native.ViewModels;

namespace OpenBullet2.Native.Tests;

[Collection("WPF")]
public sealed class DependencyInjectionTests(WpfAppFixture fixture)
{
    [Fact]
    public async Task ResolveServices_CriticalServices_AreRegistered()
    {
        await fixture.InvokeAsync(services =>
        {
            var uiFactory = services.GetRequiredService<IUiFactory>();
            var configService = services.GetRequiredService<ConfigService>();
            var configsViewModel = services.GetRequiredService<ConfigsViewModel>();

            Assert.NotNull(uiFactory);
            Assert.NotNull(configService);
            Assert.NotNull(configsViewModel);
        });
    }
}
