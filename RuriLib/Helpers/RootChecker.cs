using System.Runtime.InteropServices;
using System.Security.Principal;

namespace RuriLib.Helpers;

public static class RootChecker
{
    [DllImport("libc")]
    private static extern uint geteuid();

    public static bool IsRoot()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        return geteuid() == 0;
    }

    public static bool IsUnixRoot()
        => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && geteuid() == 0;
}
