using OpenBullet2.Native.Helpers;
using OpenBullet2.Native.Services;
using System.Windows;
using System.Windows.Threading;

namespace OpenBullet2.Native.Tests;

public sealed class WpfAppFixture : IDisposable
{
    private readonly Thread thread;
    private readonly Dispatcher dispatcher;

    public WpfAppFixture()
    {
        Alert.SuppressDialogs = true;

        using var ready = new ManualResetEventSlim();
        Exception? startupException = null;
        Dispatcher? createdDispatcher = null;

        thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                createdDispatcher = Dispatcher.CurrentDispatcher;
            }
            catch (Exception ex)
            {
                startupException = ex;
            }
            finally
            {
                ready.Set();
            }

            if (startupException is null)
            {
                Dispatcher.Run();
            }

            try
            {
                App.Host.Dispose();
            }
            catch
            {
            }
        })
        {
            IsBackground = true,
            Name = "OpenBullet2.Native.Tests.Wpf"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();

        if (startupException is not null)
        {
            throw new InvalidOperationException("Failed to initialize the WPF test app", startupException);
        }

        dispatcher = createdDispatcher ?? throw new InvalidOperationException("The WPF dispatcher was not created");
    }

    public Task InvokeAsync(Action<IServiceProvider> action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                action(App.Host.Services);
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }));

        return completion.Task;
    }

    public Task<T> InvokeAsync<T>(Func<IServiceProvider, T> action)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                completion.SetResult(action(App.Host.Services));
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }));

        return completion.Task;
    }

    public void Dispose()
    {
        dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
        thread.Join();
        Alert.SuppressDialogs = false;
    }
}
