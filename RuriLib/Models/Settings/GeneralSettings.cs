using RuriLib.Parallelization;
using System.Collections.Generic;

namespace RuriLib.Models.Settings;

public class GeneralSettings
{
    public ParallelizerType ParallelizerType { get; set; } = ParallelizerType.TaskBased;
    public bool LogJobActivityToFile { get; set; }
    public bool RestrictBlocksToCWD { get; set; } = true;
    public bool UseCustomUserAgentsList { get; set; }
    public bool EnableBotLogging { get; set; }
    public bool VerboseMode { get; set; }
    public bool LogAllResults { get; set; }
    public List<string> UserAgents { get; set; } = [];
}
