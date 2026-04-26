using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;

namespace OpenBullet2.Native.Updater.Helpers;

public static class FileSystemHelper
{
    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static string ResolveInstallDirectory(string? installDirectory)
    {
        var path = string.IsNullOrWhiteSpace(installDirectory)
            ? AppContext.BaseDirectory
            : installDirectory;

        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(fullPath);

        return fullPath;
    }

    private static string GetSafeInstallationPath(string installDirectory, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException($"Unsafe absolute path: {relativePath}");
        }

        var root = Path.GetFullPath(installDirectory);
        var rootWithSeparator = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!path.StartsWith(rootWithSeparator, PathComparison))
        {
            throw new InvalidOperationException($"Unsafe path outside the installation directory: {relativePath}");
        }

        return path;
    }

    public static async Task<Version?> GetLocalVersionAsync(string installDirectory)
    {
        return await AnsiConsole.Status()
            .StartAsync("[yellow]Reading the current version...[/]", async ctx =>
            {
                var versionFile = Path.Combine(installDirectory, "version.txt");

                // Check if version.txt exists
                if (!File.Exists(versionFile))
                {
                    return null;
                }

                var content = await File.ReadAllLinesAsync(versionFile);
                var currentVersion = Version.Parse(content.First());

                AnsiConsole.MarkupLineInterpolated($"[green]Current version: {currentVersion}[/]");

                return currentVersion;
            });
    }

    public static async Task CleanupInstallationFolderAsync(string installDirectory)
    {
        AnsiConsole.MarkupLine("[yellow]Cleaning up the OB2 folder...[/]");

        // The build-files.txt file contains a list of all the files in the current build.
        // We will delete all those files and folders and clean up the directory.
        var buildFiles = Path.Combine(installDirectory, "build-files.txt");
        if (File.Exists(buildFiles))
        {
            var entries = await File.ReadAllLinesAsync(buildFiles);

            AnsiConsole.Status()
                .Start("[yellow]Deleting...[/]", ctx =>
                {
                    var currentExecutable = Process.GetCurrentProcess().MainModule?.FileName;

                    foreach (var entry in entries.Where(e => !string.IsNullOrWhiteSpace(e)))
                    {
                        ctx.Status($"Deleting {entry}...");

                        // If it's appsettings.json or the UserData folder, disregard it
                        if (entry == "appsettings.json" || entry.StartsWith("UserData"))
                        {
                            continue;
                        }

                        var path = GetSafeInstallationPath(installDirectory, entry);

                        // If it's the current executable, disregard it
                        if (string.Equals(path, currentExecutable, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        else if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }
                    }
                });
        }
        // If the file does not exist, skip the deletion
        else
        {
            AnsiConsole.MarkupLine(
                "[yellow]build-files.txt not found, skipping file deletion...[/]");
        }
    }

    public static async Task ExtractArchiveAsync(Stream stream, string installDirectory)
    {
        AnsiConsole.MarkupLine("[yellow]Extracting the archive...[/]");

        await AnsiConsole.Status()
            .StartAsync("[yellow]Extracting...[/]", async ctx =>
            {
                using var archive = new ZipArchive(stream);
                foreach (var entry in archive.Entries)
                {
                    // Do not extract appsettings.json if it exists
                    if (entry.FullName.Contains("appsettings.json") &&
                        File.Exists(Path.Combine(installDirectory, "appsettings.json")))
                    {
                        continue;
                    }

                    // Do not extract anything in the UserData folder (important)
                    if (entry.FullName.StartsWith("UserData"))
                    {
                        continue;
                    }

                    // If the entry is a directory, disregard it
                    if (entry.FullName.EndsWith('/'))
                    {
                        continue;
                    }

                    ctx.Status($"Extracting {entry.FullName}...");

                    var path = GetSafeInstallationPath(installDirectory, entry.FullName);
                    var dir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir!);
                    }

                    await using var fileStream = new FileStream(path, FileMode.Create);
                    await using var entryStream = entry.Open();
                    await entryStream.CopyToAsync(fileStream);
                }

                await stream.DisposeAsync();
            });
    }
}
