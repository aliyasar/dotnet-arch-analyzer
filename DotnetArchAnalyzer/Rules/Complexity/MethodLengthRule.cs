using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules.Complexity;

/// <summary>
/// Detects methods that are too long (too many lines of code).
/// Long methods are hard to read, test, and maintain.
///
/// Threshold is configurable via dotnetarch.json:
///   "thresholds": { "complexity/method-length": 30 }
/// </summary>
public sealed class MethodLengthRule : IArchRule
{
    public const int DefaultThreshold = 30;
    public string RuleId => "complexity/method-length";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var threshold = config.GetThreshold(RuleId, DefaultThreshold);
        var severity  = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

        foreach (var method in methods.Where(m => m.LineCount > threshold))
        {
            yield return new AnalysisViolation(
                severity,
                RuleId,
                $"{method.TypeName}.{method.MethodName} is {method.LineCount} lines long (threshold: {threshold})",
                method.FilePath,
                method.Line);
        }
    }
}
