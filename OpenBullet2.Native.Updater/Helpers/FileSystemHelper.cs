using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenBullet2.Native.Updater.Helpers;

public static class FileSystemHelper
{
    public static string ResolveInstallDirectory(string? installDirectory)
        => OpenBullet2.Updater.Core.Helpers.FileSystemHelper.ResolveInstallDirectory(installDirectory);

    public static Task<Version?> GetLocalVersionAsync(string installDirectory)
        => OpenBullet2.Updater.Core.Helpers.FileSystemHelper.GetLocalVersionAsync(installDirectory);

    public static Task ApplyUpdateAsync(Stream stream, string installDirectory)
        => OpenBullet2.Updater.Core.Helpers.FileSystemHelper.ApplyUpdateAsync(stream, installDirectory);
}
