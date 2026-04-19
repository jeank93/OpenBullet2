using RuriLib.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Helpers;

public class PauseTokenSourceTests
{
    [Fact]
    public async Task PauseAsync_ConsumerAcknowledgesPause_MarksSourceAsPaused()
    {
        var pts = new PauseTokenSource();

        var pauseTask = pts.PauseAsync();
        Assert.False(pauseTask.IsCompleted);

        // The consumer confirms the pause request before the source can enter the paused state.
        var pausedConsumerTask = pts.Token.PauseIfRequestedAsync();
        await pauseTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(await pts.IsPausedAsync());
        Assert.False(pausedConsumerTask.IsCompleted);

        await pts.ResumeAsync();
        await pausedConsumerTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PauseIfRequestedAsync_PausedThenResumed_WaitsUntilResume()
    {
        var pts = new PauseTokenSource();
        var pauseTask = pts.PauseAsync();
        var pausedConsumerTask = pts.Token.PauseIfRequestedAsync();

        await pauseTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(await pts.IsPausedAsync());
        Assert.False(pausedConsumerTask.IsCompleted);

        await pts.ResumeAsync();
        await pausedConsumerTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(await pts.IsPausedAsync());
    }

    [Fact]
    public async Task PauseIfRequestedAsync_WhenNotPaused_CompletesImmediately()
    {
        var pts = new PauseTokenSource();

        await pts.Token.PauseIfRequestedAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(await pts.IsPausedAsync());
    }

    [Fact]
    public async Task ResumeAsync_WhenNotPaused_CompletesWithoutChangingState()
    {
        var pts = new PauseTokenSource();

        await pts.ResumeAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(await pts.IsPausedAsync());
    }
}
