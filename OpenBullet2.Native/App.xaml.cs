using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenBullet2.Core;
using OpenBullet2.Core.Repositories;
using OpenBullet2.Core.Services;
using OpenBullet2.Logging;
using OpenBullet2.Native.Helpers;
using OpenBullet2.Native.Services;
using OpenBullet2.Native.Utils;
using OpenBullet2.Native.ViewModels;
using OpenBullet2.Native.Views.Dialogs;
using OpenBullet2.Native.Views.Pages;
using OpenBullet2.Native.Views.Pages.Shared;
using RuriLib.Logging;
using RuriLib.Providers.RandomNumbers;
using RuriLib.Providers.UserAgents;
using RuriLib.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using OpenBullet2.Core.Models.Proxies;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace OpenBullet2.Native;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string LogsPath = "UserData/Logs/log-.txt";
    private readonly IConfiguration config;
    public static IHost Host { get; private set; } = null!;

    public App()
    {
        Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskException;

        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        Directory.CreateDirectory("UserData");

        config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var workerThreads = config.GetSection("Resources").GetValue("WorkerThreads", 1000);
        var ioThreads = config.GetSection("Resources").GetValue("IOThreads", 1000);

        ThreadPool.SetMinThreads(workerThreads, ioThreads);

        Host = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(config);
                ConfigureServices(services);
            })
            .UseDefaultServiceProvider((_, options) =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            })
            .Build();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogsPath) ?? "UserData/Logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.File(new CompactJsonFormatter(), LogsPath,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 1_000_000)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        // Windows and pages
        services.AddSingleton<IUiFactory, UiFactory>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        // EF
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("OpenBullet2.Core")));

        // Repositories
        services.AddScoped<IProxyRepository, DbProxyRepository>();
        services.AddScoped<IProxyGroupRepository, DbProxyGroupRepository>();
        services.AddScoped<IHitRepository, DbHitRepository>();
        services.AddScoped<IJobRepository, DbJobRepository>();
        services.AddScoped<IRecordRepository, DbRecordRepository>();
        services.AddSingleton<IConfigRepository>(service =>
            new DiskConfigRepository(service.GetRequiredService<RuriLibSettingsService>(),
            "UserData/Configs"));
        services.AddScoped<IWordlistRepository>(service =>
            new HybridWordlistRepository(service.GetRequiredService<ApplicationDbContext>(),
            "UserData/Wordlists"));

        // Singletons
        services.AddSingleton<VolatileSettingsService>();
        services.AddSingleton<AnnouncementService>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<ConfigService>();
        services.AddSingleton<ProxyReloadService>();
        services.AddSingleton<ProxyCheckOutputFactory>();
        services.AddSingleton<JobFactoryService>();
        services.AddSingleton<TriggeredActionExecutor>();
        services.AddSingleton<JobManagerService>();
        services.AddSingleton<JobMonitorService>();
        services.AddSingleton<HitStorageService>();
        services.AddSingleton<DataPoolFactoryService>();
        services.AddSingleton<ProxySourceFactoryService>();
        services.AddSingleton(_ => new RuriLibSettingsService("UserData"));
        services.AddSingleton(_ => new OpenBulletSettingsService("UserData"));
        services.AddSingleton(_ => new PluginRepository("UserData/Plugins"));
        services.AddSingleton<IRandomUAProvider>(_ => new IntoliRandomUAProvider("user-agents.json"));
        services.AddSingleton<IRNGProvider, DefaultRNGProvider>();
        services.AddSingleton<MemoryJobLogger>();
        services.AddSingleton<IJobLogger>(service =>
            new FileJobLogger(service.GetRequiredService<RuriLibSettingsService>(),
            "UserData/Logs/Jobs"));

        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<JobsViewModel>();
        services.AddSingleton<ProxiesViewModel>();
        services.AddSingleton<WordlistsViewModel>();
        services.AddSingleton<ConfigsViewModel>();
        services.AddSingleton<HitsViewModel>();
        services.AddSingleton<OBSettingsViewModel>();
        services.AddSingleton<RLSettingsViewModel>();
        services.AddSingleton<PluginsViewModel>();
        services.AddSingleton<ConfigMetadataViewModel>();
        services.AddSingleton<ConfigReadmeViewModel>();
        services.AddSingleton<ConfigStackerViewModel>();
        services.AddSingleton<ConfigSettingsViewModel>();
        services.AddSingleton<DebuggerViewModel>();

        services.AddTransient<ConfigEditorViewModel>();
        services.AddTransient<ConfigLoliCodeViewModel>();
        services.AddTransient<ConfigCSharpCodeViewModel>();
        services.AddTransient<AddBlockDialogViewModel>();
        services.AddTransient<SelectConfigDialogViewModel>();
        services.AddTransient<SelectWordlistDialogViewModel>();
        services.AddTransient<MultiRunJobOptionsViewModel>();
        services.AddTransient<ProxyCheckJobOptionsViewModel>();

        services.AddTransient<Debugger>();
        services.AddTransient<ConfigStacker>();
        services.AddTransient<ConfigLoliCode>();
        services.AddTransient<ConfigCSharpCode>();
        services.AddTransient<ConfigLoliScript>();
    }

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        try
        {
            await Host.StartAsync();

            using (var serviceScope = Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();
            }

            var configService = Host.Services.GetRequiredService<ConfigService>();
            await configService.ReloadConfigsAsync();

            AutocompletionProvider.Init(Host.Services.GetRequiredService<OpenBulletSettingsService>());
            Suggestions.Init(
                Host.Services.GetRequiredService<DebuggerViewModel>(),
                Host.Services.GetRequiredService<RuriLibSettingsService>(),
                Host.Services.GetRequiredService<ConfigService>());

            _ = Host.Services.GetRequiredService<JobMonitorService>();

            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            mainWindow.NavigateTo(MainWindowPage.Home);
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            ReportCrash(ex);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await Host.StopAsync();
        Host.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportCrash(e.Exception);
        e.Handled = true; // Set to false to close the app on exception
    }

    private void OnTaskException(object? sender, UnobservedTaskExceptionEventArgs e) =>
        e.SetObserved(); // Comment this line to close the app on task exception.

    // I decided to disable the code below since usually task exceptions
    // are not critical to the application.

    /*
    if (e.Exception.InnerException is not null)
    {
        if (e.Exception.InnerException is PuppeteerSharp.PuppeteerException // https://github.com/hardkoded/puppeteer-sharp/issues/891
            or System.Net.Sockets.SocketException // Seems like all networking-related things can cause unhandled task exceptions
            or TimeoutException) // This is again thrown by Puppeteer
        {
            return;
        }
    }

    ReportCrash(e.Exception);
    */

    private static void ReportCrash(Exception ex)
    {
        var crashLogPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
        var copyText = ex.ToString();

        Log.Fatal(ex, "Unhandled exception");
        File.WriteAllText(crashLogPath, $"Unhandled exception thrown on {DateTime.Now}{Environment.NewLine}{ex}");

        Alert.Error("Unhandled exception", $"An unhandled exception was thrown, the application will try to continue running." +
            " A crash log was written next to the executable." +
            $" A few details about the exception: {ex.Message}", copyText);
    }
}
