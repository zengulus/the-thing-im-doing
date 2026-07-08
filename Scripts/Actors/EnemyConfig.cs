using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Godot;
using TheThingImDoing.Content;

namespace TheThingImDoing.Actors;

public sealed record EnemyConfig(
    string Id,
    string DisplayNameKey,
    string SigilKey,
    int MaxHealth,
    string PurposeKey,
    string BehaviorId,
    string DefaultIntentKey,
    string TintHex,
    IReadOnlyList<EnemyIntentRule> IntentRules,
    IReadOnlyList<string> Tags)
{
    public string DisplayName => GameStrings.Get(DisplayNameKey);
    public string Sigil => GameStrings.Get(SigilKey);
    public string Purpose => GameStrings.Get(PurposeKey);
    public string DefaultIntent => GameStrings.Get(DefaultIntentKey);
    public Color Tint => ParseColor(TintHex);

    private static Color ParseColor(string hex)
    {
        if (hex.Length == 7
            && hex[0] == '#'
            && int.TryParse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int red)
            && int.TryParse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int green)
            && int.TryParse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int blue))
        {
            return new Color(red / 255.0f, green / 255.0f, blue / 255.0f);
        }

        return new Color(0.90f, 0.35f, 0.32f);
    }
}

public sealed record EnemyIntentRule(string When, string IntentKey, int? Amount = null, string Counter = "");

public static class EnemyConfigCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, EnemyConfig>> Configs = new(LoadConfigs);

    public static IEnumerable<EnemyConfig> All => Configs.Value.Values;

    public static EnemyConfig Get(string id)
    {
        return Configs.Value[id];
    }

    public static bool TryGet(string id, [NotNullWhen(true)] out EnemyConfig? config)
    {
        return Configs.Value.TryGetValue(id, out config);
    }

    private static IReadOnlyDictionary<string, EnemyConfig> LoadConfigs()
    {
        var configs = new Dictionary<string, EnemyConfig>(StringComparer.Ordinal);

        foreach (EnemyContentDefinition contentDefinition in ContentJsonLoader.LoadItems<EnemyContentFile, EnemyContentDefinition>(
                     "enemies.json",
                     file => file.Enemies))
        {
            configs[contentDefinition.Id] = new EnemyConfig(
                contentDefinition.Id,
                contentDefinition.DisplayNameKey,
                contentDefinition.SigilKey,
                contentDefinition.MaxHealth,
                contentDefinition.PurposeKey,
                contentDefinition.BehaviorId,
                contentDefinition.DefaultIntentKey,
                contentDefinition.TintHex,
                contentDefinition.IntentRules
                    .Select(rule => new EnemyIntentRule(rule.When, rule.IntentKey, rule.Amount, rule.Counter))
                    .ToArray(),
                contentDefinition.Tags);
        }

        return configs
            .OrderBy(pair => pair.Value.DisplayName, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private sealed class EnemyContentFile
    {
        public int SchemaVersion { get; set; }
        public List<EnemyContentDefinition> Enemies { get; set; } = [];
    }

    private sealed class EnemyContentDefinition
    {
        public string Id { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public string SigilKey { get; set; } = "";
        public int MaxHealth { get; set; }
        public string PurposeKey { get; set; } = "";
        public string BehaviorId { get; set; } = "";
        public string DefaultIntentKey { get; set; } = "";
        public string TintHex { get; set; } = "#e65a52";
        public List<EnemyIntentContentRule> IntentRules { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    private sealed class EnemyIntentContentRule
    {
        public string When { get; set; } = "";
        public string IntentKey { get; set; } = "";
        public int? Amount { get; set; }
        public string Counter { get; set; } = "";
    }
}
