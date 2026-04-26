using System;
using System.Threading.Tasks;
using OpenBullet2.Updater.Core.Helpers;
using Spectre.Console;

namespace OpenBullet2.Native.Updater.Helpers;

public static class RequirementsChecker
{
    private static readonly Version _dotnetVersion = new(8, 0);

    public static async Task EnsureOb2NativeNotRunningAsync()
        => await UpdaterRequirements.EnsureProcessNotRunningAsync("OpenBullet2.Native");

    /// <summary>
    /// Checks if the .NET Windows Desktop Runtime is installed. If the user installed the SDK,
    /// it will still work because the runtime is included in the SDK.
    /// </summary>
    public static async Task EnsureDotNetInstalledAsync()
    {
        if (await UpdaterRequirements.IsRuntimeInstalledAsync("Microsoft.WindowsDesktop.App", _dotnetVersion))
        {
            return;
        }

        var installRuntime = AnsiConsole.Prompt(
            new ConfirmationPrompt($"The .NET Windows Desktop Runtime version {_dotnetVersion} is required to run OpenBullet 2. " +
                                   "Do you want to download and install it now?"));

        if (!installRuntime)
        {
            Utils.ExitWithError($"The .NET Windows Desktop Runtime version {_dotnetVersion} is required to run OpenBullet 2. " +
                                $"Please install it from https://dotnet.microsoft.com/en-us/download/dotnet/{_dotnetVersion} " +
                                "and relaunch the Updater");
        }

        await InstallDotNetRuntimeAsync();
    }

    // The .NET Windows Desktop Runtime also includes the .NET Runtime
    private static async Task InstallDotNetRuntimeAsync()
    {
        await UpdaterRequirements.InstallRuntimeAsync(
            UpdaterRequirements.GetRuntimeFileName("windowsdesktop-runtime"), _dotnetVersion);
    }
}
