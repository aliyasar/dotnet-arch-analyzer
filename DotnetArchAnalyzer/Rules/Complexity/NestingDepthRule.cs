using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules.Complexity;

/// <summary>
/// Detects methods with deeply nested control flow structures.
/// Deep nesting makes code hard to read and test.
/// Counts nesting of: if, while, for, foreach, try, switch.
///
/// Threshold is configurable via dotnetarch.json:
///   "thresholds": { "complexity/nesting-depth": 4 }
/// </summary>
public sealed class NestingDepthRule : IArchRule
{
    public const int DefaultThreshold = 4;
    public string RuleId => "complexity/nesting-depth";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var threshold = config.GetThreshold(RuleId, DefaultThreshold);
        var severity  = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

        foreach (var method in methods.Where(m => m.MaxNestingDepth > threshold))
        {
            yield return new AnalysisViolation(
                severity,
                RuleId,
                $"{method.TypeName}.{method.MethodName} has nesting depth {method.MaxNestingDepth} (threshold: {threshold})",
                method.FilePath,
                method.Line);
        }
    }
}
