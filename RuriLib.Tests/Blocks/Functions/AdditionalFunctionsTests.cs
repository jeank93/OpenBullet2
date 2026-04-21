using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Configs;
using RuriLib.Models.Conditions.Comparisons;
using RuriLib.Models.Data;
using RuriLib.Models.Environment;
using RuriLib.Providers.Proxies;
using RuriLib.Tests.Utils.Mockup;
using System;
using System.Collections.Generic;
using Xunit;

namespace RuriLib.Tests.Blocks.Functions;

public class AdditionalFunctionsTests
{
    [Fact]
    public void MergeByteArrays_AppendsSecondArrayAfterFirst()
    {
        var data = NewBotData();

        var merged = global::RuriLib.Blocks.Functions.ByteArray.Methods.MergeByteArrays(data, [0x01, 0x02], [0x03, 0x04]);

        Assert.Equal([0x01, 0x02, 0x03, 0x04], merged);
    }

    [Fact]
    public void ConstantList_ReturnsClone()
    {
        var data = NewBotData();
        var source = new List<string> { "alpha", "beta" };

        var cloned = global::RuriLib.Blocks.Functions.Constants.Methods.ConstantList(data, source);
        source[0] = "changed";

        Assert.Equal(["alpha", "beta"], cloned);
    }

    [Fact]
    public void RandomInteger_IncludesUpperBound()
    {
        var data = NewBotData();

        var value = global::RuriLib.Blocks.Functions.Integer.Methods.RandomInteger(data, 5, 5);

        Assert.Equal(5, value);
    }

    [Fact]
    public void RandomFloat_WithSameBounds_ReturnsBound()
    {
        var data = NewBotData();

        var value = global::RuriLib.Blocks.Functions.Float.Methods.RandomFloat(data, 2.5f, 2.5f);

        Assert.Equal(2.5f, value);
    }

    [Fact]
    public void Compute_EvaluatesExpression()
    {
        var data = NewBotData();

        var value = global::RuriLib.Blocks.Functions.Float.Methods.Compute(data, "3*(2+1)");

        Assert.Equal(9f, value);
    }

    [Fact]
    public void DictionaryMethods_GetKeyReturnsEmptyStringWhenNotFound()
    {
        var data = NewBotData();

        var key = global::RuriLib.Blocks.Functions.Dictionary.Methods.GetKey(
            data,
            new Dictionary<string, string> { ["alpha"] = "1" },
            "missing");

        Assert.Equal(string.Empty, key);
    }

    [Fact]
    public void DictionaryMethods_AddAndRemoveByKey_UpdateDictionary()
    {
        var data = NewBotData();
        var dictionary = new Dictionary<string, string>();

        global::RuriLib.Blocks.Functions.Dictionary.Methods.AddKeyValuePair(data, dictionary, "alpha", "1");
        global::RuriLib.Blocks.Functions.Dictionary.Methods.RemoveByKey(data, dictionary, "alpha");

        Assert.Empty(dictionary);
    }

    [Fact]
    public void CheckCondition_StringComparison_ReturnsTrueForContains()
    {
        var data = NewBotData();

        var result = global::RuriLib.Blocks.Conditions.Methods.CheckCondition(data, "alphabet", StrComparison.Contains, "pha");

        Assert.True(result);
    }

    [Fact]
    public void CheckCondition_ListComparison_ReturnsTrueForExactElement()
    {
        var data = NewBotData();

        var result = global::RuriLib.Blocks.Conditions.Methods.CheckCondition(data, ["alpha", "beta"], ListComparison.Contains, "beta");

        Assert.True(result);
    }

    [Fact]
    public void CheckCondition_DictionaryComparison_ReturnsTrueForContainsValue()
    {
        var data = NewBotData();

        var result = global::RuriLib.Blocks.Conditions.Methods.CheckCondition(
            data,
            new Dictionary<string, string> { ["alpha"] = "one" },
            DictComparison.HasValue,
            "one");

        Assert.True(result);
    }

    [Fact]
    public void CheckGlobalBanKeys_ReturnsTrueWhenProviderMatches()
    {
        var data = NewBotData(new MatchingProxySettingsProvider("BAN", string.Empty));
        data.SOURCE = "prefix BAN suffix";

        var result = global::RuriLib.Blocks.Conditions.Methods.CheckGlobalBanKeys(data);

        Assert.True(result);
    }

    [Fact]
    public void CheckGlobalRetryKeys_ReturnsTrueWhenProviderMatches()
    {
        var data = NewBotData(new MatchingProxySettingsProvider(string.Empty, "RETRY"));
        data.SOURCE = "prefix RETRY suffix";

        var result = global::RuriLib.Blocks.Conditions.Methods.CheckGlobalRetryKeys(data);

        Assert.True(result);
    }

    private static BotData NewBotData(IProxySettingsProvider? proxySettingsProvider = null)
        => new(
            new global::RuriLib.Models.Bots.Providers(null!)
            {
                ProxySettings = proxySettingsProvider ?? new MockedProxySettingsProvider(),
                Security = new MockedSecurityProvider()
            },
            new ConfigSettings(),
            new BotLogger(),
            new DataLine("hello", new WordlistType()));

    private sealed class MatchingProxySettingsProvider(string banKey, string retryKey) : IProxySettingsProvider
    {
        public TimeSpan ConnectTimeout => TimeSpan.FromSeconds(10);

        public TimeSpan ReadWriteTimeout => TimeSpan.FromSeconds(10);

        public bool ContainsBanKey(string text, out string matchedKey, bool caseSensitive = false)
        {
            var found = !string.IsNullOrEmpty(banKey) && text.Contains(banKey);
            matchedKey = found ? banKey : string.Empty;
            return found;
        }

        public bool ContainsRetryKey(string text, out string matchedKey, bool caseSensitive = false)
        {
            var found = !string.IsNullOrEmpty(retryKey) && text.Contains(retryKey);
            matchedKey = found ? retryKey : string.Empty;
            return found;
        }
    }
}
