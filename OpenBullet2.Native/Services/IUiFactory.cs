using Microsoft.Extensions.DependencyInjection;
using System;

namespace OpenBullet2.Native.Services;

public interface IUiFactory
{
    T Create<T>(params object[] args) where T : class;
}

public class UiFactory(IServiceProvider serviceProvider) : IUiFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public T Create<T>(params object[] args) where T : class
        => ActivatorUtilities.CreateInstance<T>(_serviceProvider, args);
}
