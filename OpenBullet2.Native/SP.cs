using Microsoft.Extensions.DependencyInjection;
using System;

namespace OpenBullet2.Native;

public static class SP
{
    private static IServiceProvider? instance;

    public static void Init(IServiceProvider instance) => SP.instance = instance;

    public static T GetService<T>() where T : notnull
    {
        if (instance is null)
        {
            throw new InvalidOperationException("Service provider has not been initialized");
        }

        var scopeFactory = instance.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }
}
