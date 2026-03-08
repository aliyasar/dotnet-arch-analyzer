using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules.Style;

/// <summary>
/// Detects empty catch blocks: catch { } or catch (Exception) { }.
/// Empty catch blocks silently swallow exceptions and hide bugs.
///
/// Rule can be disabled or overridden:
///   "rules": { "style/no-empty-catch": "off" }
/// </summary>
public sealed class NoEmptyCatchRule : IArchRule
{
    public string RuleId => "style/no-empty-catch";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var severity = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

        foreach (var method in methods)
        {
            foreach (var catchLine in method.EmptyCatchLines)
            {
                yield return new AnalysisViolation(
                    severity,
                    RuleId,
                    $"Empty catch block in '{method.TypeName}.{method.MethodName}' — handle or log the exception",
                    method.FilePath,
                    catchLine);
            }
        }
    }
}
