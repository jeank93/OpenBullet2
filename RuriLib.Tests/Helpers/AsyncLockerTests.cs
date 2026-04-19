using RuriLib.Helpers;
using System;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Helpers;

public class AsyncLockerTests
{
    [Fact]
    public async Task Acquire_SameKey_BlocksUntilRelease()
    {
        using var locker = new AsyncLocker();

        await locker.Acquire("key");
        var secondAcquire = locker.Acquire("key");

        Assert.False(secondAcquire.IsCompleted);

        locker.Release("key");
        await secondAcquire.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Acquire_TypeAndMethod_UsesCombinedKey()
    {
        using var locker = new AsyncLocker();

        await locker.Acquire(typeof(AsyncLockerTests), nameof(Acquire_TypeAndMethod_UsesCombinedKey));
        var secondAcquire = locker.Acquire(typeof(AsyncLockerTests), nameof(Acquire_TypeAndMethod_UsesCombinedKey));

        Assert.False(secondAcquire.IsCompleted);

        locker.Release(typeof(AsyncLockerTests), nameof(Acquire_TypeAndMethod_UsesCombinedKey));
        await secondAcquire.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Release_MissingKey_ThrowsInvalidOperationException()
    {
        using var locker = new AsyncLocker();

        Assert.Throws<InvalidOperationException>(() => locker.Release("missing"));
    }
}
