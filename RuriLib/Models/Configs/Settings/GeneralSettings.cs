namespace RuriLib.Models.Configs.Settings;

public class GeneralSettings
{
    public int SuggestedBots { get; set; } = 1;
    public int MaximumCPM { get; set; }
    public bool SaveEmptyCaptures { get; set; }
    public bool ReportLastCaptchaOnRetry { get; set; }

    public string[] ContinueStatuses { get; set; } =
    [
        "SUCCESS",
        "NONE"
    ];
}
