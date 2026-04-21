using RuriLib.Extensions;
using RuriLib.Helpers;
using RuriLib.Models.Proxies.ProxySources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Models.Proxies;

/// <summary>
/// Manages a shared pool of proxies loaded from one or more sources.
/// </summary>
public class ProxyPool : IDisposable
{
    /// <summary>All the proxies currently in the pool.</summary>
    public IEnumerable<Proxy> Proxies => proxies;

    /// <summary>Checks if all proxies are banned.</summary>
    public bool AllBanned => proxies.All(p => p.ProxyStatus is ProxyStatus.Bad or ProxyStatus.Banned);

    private List<Proxy> proxies = [];
    private bool isReloadingProxies;
    private readonly List<ProxySource> sources;
    private readonly ProxyPoolOptions options;

    private readonly int minBackoff = 5000;
    private readonly int maxReloadTries = 10;
    private AsyncLocker? asyncLocker;

    /// <summary>
    /// Initializes the proxy pool given the proxy sources.
    /// </summary>
    /// <param name="sources">The proxy sources to load from.</param>
    /// <param name="options">Optional pool behavior settings.</param>
    public ProxyPool(IEnumerable<ProxySource> sources, ProxyPoolOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        this.sources = sources.ToList();
        this.options = options ?? new ProxyPoolOptions();
        asyncLocker = new();
    }

    /// <summary>
    /// Sets all the BANNED and BAD proxies status to AVAILABLE and resets their Uses.
    /// </summary>
    /// <param name="minimumBanTime">The minimum ban duration that must have elapsed before a proxy can be unbanned.</param>
    public void UnbanAll(TimeSpan minimumBanTime)
    {
        var now = DateTime.Now;
        proxies.Where(p => now > p.LastBanned + minimumBanTime).ToList().ForEach(p =>
        {
            if (p.ProxyStatus is ProxyStatus.Banned or ProxyStatus.Bad)
            {
                p.ProxyStatus = ProxyStatus.Available;
                p.BeingUsedBy = 0;
                p.TotalUses = 0;
            }
        });
    }

    /// <summary>
    /// Tries to return the first available proxy from the list
    /// (or even a Busy one if <paramref name="evenBusy"/> is true).
    /// If <paramref name="maxUses"/> is not 0, the pool will try to
    /// return a proxy that was used less than <paramref name="maxUses"/> times.
    /// Use this together with a lock if possible. Returns null if no proxy
    /// matching the required parameters was found.
    /// </summary>
    /// <param name="evenBusy">Whether busy proxies are also considered valid candidates.</param>
    /// <param name="maxUses">The maximum number of times a proxy can have been used, or 0 for no limit.</param>
    /// <returns>The selected proxy, or <see langword="null"/> if none matched.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)] //hot path
    public Proxy? GetProxy(bool evenBusy = false, int maxUses = 0)
    {
        for (var i = 0; i < proxies.Count; i++)
        {
            var px = proxies[i];
            if (evenBusy)
            {
                if (px.ProxyStatus is ProxyStatus.Available or ProxyStatus.Busy)
                {
                    if (maxUses > 0)
                    {
                        if (px.TotalUses < maxUses)
                        {
                            px.BeingUsedBy++;
                            px.ProxyStatus = ProxyStatus.Busy;
                            return px;
                        }
                    }
                    else
                    {
                        px.BeingUsedBy++;
                        px.ProxyStatus = ProxyStatus.Busy;
                        return px;
                    }
                }
            }
            else if (px.ProxyStatus == ProxyStatus.Available)
            {
                if (maxUses > 0)
                {
                    if (px.TotalUses < maxUses)
                    {
                        px.BeingUsedBy++;
                        px.ProxyStatus = ProxyStatus.Busy;
                        return px;
                    }
                }
                else
                {
                    px.BeingUsedBy++;
                    px.ProxyStatus = ProxyStatus.Busy;
                    return px;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Releases a proxy that was being used, optionally banning it.
    /// </summary>
    /// <param name="proxy">The proxy to release.</param>
    /// <param name="ban">Whether the proxy should be marked as banned.</param>
    public void ReleaseProxy(Proxy proxy, bool ban = false)
    {
        ArgumentNullException.ThrowIfNull(proxy);

        proxy.TotalUses++;
        proxy.BeingUsedBy--;

        if (ban)
        {
            proxy.ProxyStatus = ProxyStatus.Banned;
            proxy.LastBanned = DateTime.Now;
        }
        else
        {
            proxy.ProxyStatus = ProxyStatus.Available;
        }
    }

    /// <summary>
    /// Removes duplicates.
    /// </summary>
    public void RemoveDuplicates()
        => proxies = proxies.Distinct(new GenericComparer<Proxy>()).ToList();

    /// <summary>
    /// Shuffles proxies.
    /// </summary>
    public void Shuffle()
        => proxies.Shuffle();

    /// <summary>
    /// Reloads all proxies in the pool from the provided sources.
    /// </summary>
    /// <param name="shuffle">Whether the loaded proxies should be shuffled.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the reload process finishes.</returns>
    public async Task ReloadAllAsync(bool shuffle = true, CancellationToken cancellationToken = default)
    {
        if (isReloadingProxies)
        {
            return;
        }

        var locker = asyncLocker ?? throw new ObjectDisposedException(nameof(ProxyPool));

        try
        {
            isReloadingProxies = true;
            await locker.Acquire(typeof(ProxyPool), nameof(ProxyPool.ReloadAllAsync), cancellationToken).ConfigureAwait(false);
            var currentTry = 0;
            var currentBackoff = minBackoff;

            // For a maximum of 'maxReloadTries' times
            while (currentTry < maxReloadTries)
            {
                // Try to reload proxies from sources
                if (await TryReloadAllAsync(shuffle, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                // If it fails to fetch at least 1 proxy, backoff by an increasing amount (e.g. to prevent rate limiting)
                Console.WriteLine($"Failed to reload, no proxies found. Waiting {currentBackoff} ms and trying again...");
                await Task.Delay(currentBackoff, cancellationToken).ConfigureAwait(false);

                currentTry++;
                currentBackoff *= 2;
            }
        }
        finally
        {
            isReloadingProxies = false;
            locker.Release(typeof(ProxyPool), nameof(ProxyPool.ReloadAllAsync));
        }
    }

    private async Task<bool> TryReloadAllAsync(bool shuffle = true, CancellationToken cancellationToken = default)
    {
        var tasks = sources.Select(async source =>
        {
            try
            {
                return await source.GetAllAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                switch (source)
                {
                    case FileProxySource x:
                        Console.WriteLine($"Could not reload proxies from source {x.FileName}");
                        break;

                    case RemoteProxySource x:
                        Console.WriteLine($"Could not reload proxies from source {x.Url}");
                        break;

                    default:
                        Console.WriteLine("Could not reload proxies from unknown source");
                        break;
                }

                return Enumerable.Empty<Proxy>();
            }
        });

        var results = (await Task.WhenAll(tasks).ConfigureAwait(false)).SelectMany(r => r).ToList();

        // If no results, return false to trigger the backoff mechanism (do not remove existing proxies)
        if (results.Count == 0)
        {
            return false;
        }

        proxies = results
            .Where(p => options.AllowedTypes.Contains(p.Type)) // Filter by allowed types
            .ToList();

        if (shuffle)
        {
            Shuffle();
        }

        return true;
    }

    /// <summary>
    /// Releases resources owned by the proxy pool.
    /// </summary>
    public void Dispose()
    {
        if (asyncLocker is not null)
        {
            try
            {
                asyncLocker.Dispose();
                asyncLocker = null;
            }
            catch
            {
                // ignored
            }
        }
    }
}
