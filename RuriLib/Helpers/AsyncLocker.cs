using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Helpers;

public class AsyncLocker : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphores = new();

    public Task Acquire(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var semaphore = semaphores.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        return semaphore.WaitAsync(cancellationToken);
    }

    public Task Acquire(Type classType, string methodName, CancellationToken cancellationToken = default)
        => Acquire(CombineTypes(classType, methodName), cancellationToken);

    public void Release(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!semaphores.TryGetValue(key, out var semaphore))
        {
            throw new InvalidOperationException($"No semaphore exists for key '{key}'.");
        }

        semaphore.Release();
    }

    public void Release(Type classType, string methodName) => Release(CombineTypes(classType, methodName));

    private static string CombineTypes(Type classType, string methodName)
    {
        ArgumentNullException.ThrowIfNull(classType);
        ArgumentNullException.ThrowIfNull(methodName);

        return $"{classType.FullName}.{methodName}";
    }

    public void Dispose()
    {
        foreach (var semaphore in semaphores.Values)
        {
            semaphore.Dispose();
        }
    }
}
