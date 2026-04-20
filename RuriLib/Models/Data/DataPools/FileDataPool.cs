using System;
using System.IO;
using System.Linq;

namespace RuriLib.Models.Data.DataPools;

public class FileDataPool : DataPool
{
    public string FileName { get; }

    public readonly int POOL_CODE = -2;

    /// <summary>
    /// Creates a DataPool by loading lines from a file with the given <paramref name="fileName"/>.
    /// </summary>
    public FileDataPool(string fileName, string wordlistType = "Default")
    {
        ArgumentNullException.ThrowIfNull(fileName);

        FileName = fileName;
        DataList = File.ReadLines(fileName);
        Size = DataList.Count();
        WordlistType = wordlistType;
    }

    /// <inheritdoc/>
    public override void Reload()
    {
        DataList = File.ReadLines(FileName);
    }
}
