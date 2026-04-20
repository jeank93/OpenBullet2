using CaptchaSharp.Enums;

namespace RuriLib.Models.Captchas;

public class CaptchaInfo
{
    public string Id { get; set; } = string.Empty;
    public CaptchaType Type { get; set; }
}
