using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules.Style;

/// <summary>
/// Enforces the .NET convention that interface names begin with 'I'
/// followed by an uppercase letter (e.g. IUserRepository).
///
/// Rule can be disabled or overridden:
///   "rules": { "style/interface-prefix": "off" }
/// </summary>
public sealed class InterfacePrefixRule : IArchRule
{
    public string RuleId => "style/interface-prefix";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var severity = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

        foreach (var type in types.Where(t => t.Kind == TypeKind.Interface))
        {
            var name = type.Name;
            if (name.Length >= 2 && name[0] == 'I' && char.IsUpper(name[1]))
                continue;

            yield return new AnalysisViolation(
                severity,
                RuleId,
                $"Interface '{name}' should be named 'I{name}' (prefix with 'I' followed by uppercase letter)",
                type.FilePath,
                type.Line);
        }
    }
}
