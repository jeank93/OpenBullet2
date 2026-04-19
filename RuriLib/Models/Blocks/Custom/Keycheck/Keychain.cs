using System.Collections.Generic;

namespace RuriLib.Models.Blocks.Custom.Keycheck;

public class Keychain
{
    public List<Key> Keys { get; set; } = [];
    public KeychainMode Mode { get; set; } = KeychainMode.OR;
    public string ResultStatus { get; set; } = "SUCCESS";
}
