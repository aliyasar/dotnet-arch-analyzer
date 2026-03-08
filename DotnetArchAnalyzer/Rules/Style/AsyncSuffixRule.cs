using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules.Style;

/// <summary>
/// Enforces the .NET convention that async methods end with 'Async'
/// (e.g. GetUserAsync, SaveOrderAsync).
///
/// Rule can be disabled or overridden:
///   "rules": { "style/async-suffix": "off" }
/// </summary>
public sealed class AsyncSuffixRule : IArchRule
{
    public string RuleId => "style/async-suffix";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var severity = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

        foreach (var method in methods.Where(m => m.IsAsync && !m.MethodName.EndsWith("Async")))
        {
            yield return new AnalysisViolation(
                severity,
                RuleId,
                $"Async method '{method.TypeName}.{method.MethodName}' should end with 'Async'",
                method.FilePath,
                method.Line);
        }
    }
}
