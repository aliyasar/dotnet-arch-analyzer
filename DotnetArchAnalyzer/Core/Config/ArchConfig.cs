using DotnetArchAnalyzer.Core.Models;

namespace DotnetArchAnalyzer.Core.Config;

public sealed class ArchConfig
{
    /// <summary>
    /// Maps layer names (domain/application/infrastructure/presentation)
    /// to namespace keyword lists. Keywords are matched against namespace segments (case-insensitive).
    /// </summary>
    public Dictionary<string, string[]> Layers { get; init; } = [];

    /// <summary>
    /// Overrides rule severity. Values: "error", "warning", "info", "off".
    /// </summary>
    public Dictionary<string, string> Rules { get; init; } = [];

    /// <summary>
    /// Numeric thresholds for rules that need them (e.g. arch/high-coupling).
    /// Example: { "arch/high-coupling": 10 }
    /// </summary>
    public Dictionary<string, int> Thresholds { get; init; } = [];

    public bool IsRuleEnabled(string ruleId) =>
        !Rules.TryGetValue(ruleId, out var s) ||
        !s.Equals("off", StringComparison.OrdinalIgnoreCase);

    public ViolationSeverity? GetSeverityOverride(string ruleId)
    {
        if (!Rules.TryGetValue(ruleId, out var s)) return null;
        return s.ToLowerInvariant() switch
        {
            "error"   => ViolationSeverity.Error,
            "warning" => ViolationSeverity.Warning,
            "info"    => ViolationSeverity.Info,
            _         => null
        };
    }

    public int GetThreshold(string ruleId, int defaultValue) =>
        Thresholds.TryGetValue(ruleId, out var t) ? t : defaultValue;

    public static ArchConfig Default => new()
    {
        Layers = new()
        {
            ["domain"]         = ["domain", "core", "entities", "entity", "aggregates", "valueobjects"],
            ["application"]    = ["services", "application", "usecases", "usecase", "handlers", "commands", "queries"],
            ["infrastructure"] = ["repositories", "infrastructure", "data", "persistence", "storage", "database", "context", "dataaccess", "dal", "ef"],
            ["presentation"]   = ["controllers", "api", "web", "presentation", "ui", "pages", "mvc", "endpoints"],
        },
        Rules = new()
        {
            ["arch/layer-violation"]    = "warning",
            ["arch/circular-dependency"] = "error",
            ["arch/high-coupling"]       = "warning",
        },
        Thresholds = new()
        {
            ["arch/high-coupling"] = 10,
        }
    };
}
