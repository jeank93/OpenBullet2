using RuriLib.Extensions;
using RuriLib.Functions.Files;
using System.IO;
using System.Threading.Tasks;

namespace RuriLib.Models.Hits.HitOutputs;

/// <summary>
/// Stores hits on the local filesystem.
/// </summary>
public class FileSystemHitOutput : IHitOutput
{
    /// <summary>Gets or sets the base output directory.</summary>
    public string BaseDir { get; set; }

    /// <summary>
    /// Creates a filesystem hit output.
    /// </summary>
    /// <param name="baseDir">The base output directory.</param>
    public FileSystemHitOutput(string baseDir = "Hits")
    {
        BaseDir = baseDir;
    }

    /// <inheritdoc />
    public Task Store(Hit hit)
    {
        Directory.CreateDirectory(BaseDir);

        var folderName = Path.Combine(BaseDir, hit.Config.Metadata.Name.ToValidFileName());
        Directory.CreateDirectory(folderName);

        var fileName = Path.Combine(folderName, $"{hit.Type.ToValidFileName()}.txt");

        lock (FileLocker.GetHandle(fileName))
        {
            File.AppendAllText(fileName, $"{hit}{System.Environment.NewLine}");
        }

        return Task.CompletedTask;
    }
}
