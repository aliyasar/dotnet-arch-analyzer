using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules;

/// <summary>
/// Detects types with too many namespace-level dependencies (high coupling).
///
/// Threshold is configurable via dotnetarch.json:
///   "thresholds": { "arch/high-coupling": 10 }
///
/// Rule can be disabled or overridden:
///   "rules": { "arch/high-coupling": "off" }
/// </summary>
public sealed class HighCouplingRule : IArchRule
{
    public const int DefaultThreshold = 10;
    public string RuleId => "arch/high-coupling";

    public IEnumerable<AnalysisViolation> Analyze(IReadOnlyList<TypeInfo> types, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var threshold = config.GetThreshold(RuleId, DefaultThreshold);
        var severity = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

        foreach (var type in types)
        {
            if (type.DependencyCount <= threshold) continue;

            yield return new AnalysisViolation(
                severity,
                RuleId,
                $"High coupling: {type.Name} depends on {type.DependencyCount} namespaces (threshold: {threshold})",
                type.FilePath,
                type.Line);
        }
    }
}
