using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules.Complexity;

/// <summary>
/// Detects methods with too many parameters.
/// High parameter counts indicate a method is doing too much
/// or should use a parameter object / builder pattern.
///
/// Threshold is configurable via dotnetarch.json:
///   "thresholds": { "complexity/parameter-count": 5 }
/// </summary>
public sealed class ParameterCountRule : IArchRule
{
    public const int DefaultThreshold = 5;
    public string RuleId => "complexity/parameter-count";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var threshold = config.GetThreshold(RuleId, DefaultThreshold);
        var severity  = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

        foreach (var method in methods.Where(m => m.ParameterCount > threshold))
        {
            yield return new AnalysisViolation(
                severity,
                RuleId,
                $"{method.TypeName}.{method.MethodName} has {method.ParameterCount} parameters (threshold: {threshold})",
                method.FilePath,
                method.Line);
        }
    }
}
