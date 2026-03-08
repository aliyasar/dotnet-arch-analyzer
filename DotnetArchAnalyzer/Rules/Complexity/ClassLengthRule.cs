using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules.Complexity;

/// <summary>
/// Detects types that are too long (too many lines of code).
/// Large classes often violate the Single Responsibility Principle.
///
/// Threshold is configurable via dotnetarch.json:
///   "thresholds": { "complexity/class-length": 300 }
/// </summary>
public sealed class ClassLengthRule : IArchRule
{
    public const int DefaultThreshold = 300;
    public string RuleId => "complexity/class-length";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var threshold = config.GetThreshold(RuleId, DefaultThreshold);
        var severity  = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

        foreach (var type in types.Where(t => t.LineCount > threshold))
        {
            yield return new AnalysisViolation(
                severity,
                RuleId,
                $"{type.Name} is {type.LineCount} lines long (threshold: {threshold})",
                type.FilePath,
                type.Line);
        }
    }
}
