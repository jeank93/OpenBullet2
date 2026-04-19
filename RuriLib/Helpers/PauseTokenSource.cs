using System;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Helpers;

// PauseTokenSource. Code from https://stackoverflow.com/questions/19613444/a-pattern-to-pause-resume-an-async-task
public class PauseTokenSource
{
    private bool paused;
    private bool pauseRequested;

    private TaskCompletionSource<bool>? resumeRequestTcs;
    private TaskCompletionSource<bool>? pauseConfirmationTcs;

    private readonly SemaphoreSlim stateAsyncLock = new(1);
    private readonly SemaphoreSlim pauseRequestAsyncLock = new(1);

    public PauseToken Token => new(this);

    public async Task<bool> IsPausedAsync(CancellationToken token = default)
    {
        await stateAsyncLock.WaitAsync(token);

        try
        {
            return paused;
        }
        finally
        {
            stateAsyncLock.Release();
        }
    }

    public async Task ResumeAsync(CancellationToken token = default)
    {
        await stateAsyncLock.WaitAsync(token);

        try
        {
            if (!paused)
            {
                return;
            }

            await pauseRequestAsyncLock.WaitAsync(token);

            try
            {
                var pendingResumeRequest = resumeRequestTcs;
                paused = false;
                pauseRequested = false;
                resumeRequestTcs = null;
                pauseConfirmationTcs = null;
                pendingResumeRequest?.TrySetResult(true);
            }
            finally
            {
                pauseRequestAsyncLock.Release();
            }
        }
        finally
        {
            stateAsyncLock.Release();
        }
    }

    public async Task PauseAsync(CancellationToken token = default)
    {
        await stateAsyncLock.WaitAsync(token);

        try
        {
            if (paused)
            {
                return;
            }

            Task? pauseConfirmationTask = null;
            await pauseRequestAsyncLock.WaitAsync(token);

            try
            {
                pauseRequested = true;
                resumeRequestTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                pauseConfirmationTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                pauseConfirmationTask = WaitForPauseConfirmationAsync(token);
            }
            finally
            {
                pauseRequestAsyncLock.Release();
            }

            await pauseConfirmationTask;
            paused = true;
        }
        finally
        {
            stateAsyncLock.Release();
        }
    }

    private async Task WaitForResumeRequestAsync(CancellationToken token)
    {
        var pendingResumeRequest = resumeRequestTcs
            ?? throw new InvalidOperationException("A resume request cannot be awaited before pause is requested.");

        await using (token.Register(() => pendingResumeRequest.TrySetCanceled(token), useSynchronizationContext: false))
        {
            await pendingResumeRequest.Task;
        }
    }

    private async Task WaitForPauseConfirmationAsync(CancellationToken token)
    {
        var pendingPauseConfirmation = pauseConfirmationTcs
            ?? throw new InvalidOperationException("Pause confirmation cannot be awaited before pause is requested.");

        await using (token.Register(() => pendingPauseConfirmation.TrySetCanceled(token), useSynchronizationContext: false))
        {
            await pendingPauseConfirmation.Task;
        }
    }

    public async Task PauseIfRequestedAsync(CancellationToken token = default)
    {
        Task? resumeRequestTask = null;

        await pauseRequestAsyncLock.WaitAsync(token);

        try
        {
            if (!pauseRequested)
            {
                return;
            }

            // Confirm that the producer observed the pause request, then wait for resume.
            pauseConfirmationTcs?.TrySetResult(true);
            resumeRequestTask = WaitForResumeRequestAsync(token);
        }
        finally
        {
            pauseRequestAsyncLock.Release();
        }

        await resumeRequestTask;
    }
}

// PauseToken - consumer side
public readonly struct PauseToken
{
    private readonly PauseTokenSource source;

    public PauseToken(PauseTokenSource source)
    {
        this.source = source;
    }

    public Task<bool> IsPaused() => source.IsPausedAsync();

    public Task PauseIfRequestedAsync(CancellationToken token = default)
        => source.PauseIfRequestedAsync(token);
}
