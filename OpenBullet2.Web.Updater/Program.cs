using CommandLine;
using System;
using System.Threading.Tasks;
using OpenBullet2.Updater.Core;
using OpenBullet2.Updater.Core.Helpers;
using OpenBullet2.Web.Updater.Helpers;

namespace OpenBullet2.Web.Updater;

public static class Program
{
    private static readonly UpdaterSettings Settings = new(
        "OpenBullet2.Web.zip",
        RequirementsChecker.EnsureOb2WebNotRunningAsync,
        RequirementsChecker.EnsureDotNetInstalledAsync);

    private static async Task Main(string[] args)
    {
        try
        {
            await new Parser(with => { with.CaseInsensitiveEnumValues = true; }).ParseArguments<CliOptions>(args)
                .WithParsedAsync(async opts => await UpdaterRunner.UpdateAsync(opts, Settings));
        }
        catch (Exception ex)
        {
            Utils.ExitWithError(ex);
        }
    }
}
