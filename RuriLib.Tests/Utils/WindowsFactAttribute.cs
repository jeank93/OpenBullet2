using System;
using Xunit;

namespace RuriLib.Tests.Utils;

public sealed class WindowsFactAttribute : SkippableFactAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Requires Windows GDI+ support";
        }
    }
}
