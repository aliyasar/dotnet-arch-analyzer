using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules.Style;

/// <summary>
/// Detects types whose namespace does not match their folder location.
/// Checks that the last directory segment of the file path appears somewhere
/// in the namespace (case-insensitive).
///
/// Example violation:
///   File:      src/Controllers/HomeController.cs
///   Namespace: MyApp.Services  (missing "Controllers")
///
/// Rule can be disabled or overridden:
///   "rules": { "style/namespace-match": "off" }
/// </summary>
public sealed class NamespaceMatchRule : IArchRule
{
    public string RuleId => "style/namespace-match";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var severity = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

        // Track reported files to avoid duplicate violations for multiple types in same file
        var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in types)
        {
            if (string.IsNullOrEmpty(type.Namespace)) continue;
            if (string.IsNullOrEmpty(type.FilePath))  continue;

            var dir = Path.GetDirectoryName(type.FilePath);
            if (string.IsNullOrEmpty(dir)) continue;

            // Get the immediate parent folder name
            var folderName = Path.GetFileName(dir);
            if (string.IsNullOrEmpty(folderName)) continue;

            // Skip common root-level folders that don't map to namespaces
            if (folderName.Equals("src", StringComparison.OrdinalIgnoreCase) ||
                folderName.Equals("lib", StringComparison.OrdinalIgnoreCase))
                continue;

            // Check if folder segment appears in namespace
            var namespaceParts = type.Namespace.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var matches = namespaceParts.Any(p =>
                p.Equals(folderName, StringComparison.OrdinalIgnoreCase));

            if (!matches && reported.Add(type.FilePath))
            {
                yield return new AnalysisViolation(
                    severity,
                    RuleId,
                    $"Namespace '{type.Namespace}' does not match folder '{folderName}' — consider renaming the namespace",
                    type.FilePath,
                    type.Line);
            }
        }
    }
}
