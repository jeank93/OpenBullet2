using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenBullet2.Updater.Core.Helpers;
using Spectre.Console;

namespace OpenBullet2.Web.Updater.Helpers;

public static class RequirementsChecker
{
    private static readonly Version _dotnetVersion = new(8, 0);

    public static async Task EnsureOb2WebNotRunningAsync()
        => await UpdaterRequirements.EnsureProcessNotRunningAsync("OpenBullet2.Web");

    /// <summary>
    /// Checks if the ASP.NET Core Runtime is installed. If the user installed the SDK,
    /// it will still work because the runtime is included in the SDK.
    /// </summary>
    public static async Task EnsureDotNetInstalledAsync()
    {
        if (await UpdaterRequirements.IsRuntimeInstalledAsync("Microsoft.AspNetCore.App", _dotnetVersion))
        {
            return;
        }

        var installRuntime = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && AnsiConsole.Prompt(
                new ConfirmationPrompt($"The .NET Runtime and ASP.NET Core Runtime version {_dotnetVersion} are required to run OpenBullet 2. " +
                                       "Do you want to download and install them now?"));

        if (!installRuntime)
        {
            Utils.ExitWithError($"The .NET Runtime and ASP.NET Core Runtime version {_dotnetVersion} are required to run OpenBullet 2. " +
                                $"Please install them from https://dotnet.microsoft.com/en-us/download/dotnet/{_dotnetVersion} " +
                                "and relaunch the Updater");
        }

        await InstallDotNetRuntimeAsync();
    }

    // We need to install both the .NET Runtime and the ASP.NET Core Runtime
    private static async Task InstallDotNetRuntimeAsync()
    {
        // Download and install the .NET Runtime
        await UpdaterRequirements.InstallRuntimeAsync(
            UpdaterRequirements.GetRuntimeFileName("dotnet-runtime"), _dotnetVersion);

        // Download and install the ASP.NET Core Runtime
        await UpdaterRequirements.InstallRuntimeAsync(
            UpdaterRequirements.GetRuntimeFileName("aspnetcore-runtime"), _dotnetVersion);
    }
}
