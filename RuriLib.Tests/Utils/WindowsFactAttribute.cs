using System;
using Xunit;

namespace RuriLib.Tests.Utils;

public sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute(string? sourceFilePath = null, int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Requires Windows GDI+ support";
        }
    }
}
