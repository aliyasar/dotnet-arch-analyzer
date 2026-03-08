using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules.Complexity;

/// <summary>
/// Detects methods with high cyclomatic complexity (McCabe complexity).
/// Counts decision points: if, while, for, foreach, case, catch,
/// ternary (?:), logical && and ||.
///
/// High complexity = hard to test and maintain.
/// A value above 10 typically indicates a method should be refactored.
///
/// Threshold is configurable via dotnetarch.json:
///   "thresholds": { "complexity/cyclomatic": 10 }
/// </summary>
public sealed class CyclomaticComplexityRule : IArchRule
{
    public const int DefaultThreshold = 10;
    public string RuleId => "complexity/cyclomatic";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var threshold = config.GetThreshold(RuleId, DefaultThreshold);
        var severity  = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

        foreach (var method in methods.Where(m => m.CyclomaticComplexity > threshold))
        {
            yield return new AnalysisViolation(
                severity,
                RuleId,
                $"{method.TypeName}.{method.MethodName} has cyclomatic complexity {method.CyclomaticComplexity} (threshold: {threshold})",
                method.FilePath,
                method.Line);
        }
    }
}
