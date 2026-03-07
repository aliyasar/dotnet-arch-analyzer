using DotnetArchAnalyzer.Core.Models;

namespace DotnetArchAnalyzer.Reporting;

/// <summary>
/// Generates a Mermaid diagram of namespace-level dependencies.
/// Output can be pasted into any Mermaid renderer (GitHub, mermaid.live, etc.)
/// </summary>
public sealed class MermaidReporter
{
    public string Generate(AnalysisResult result)
    {
        var internalNamespaces = result.Types
            .Select(t => t.Namespace)
            .Where(ns => !string.IsNullOrEmpty(ns))
            .ToHashSet();

        var edges = new HashSet<(string From, string To)>();

        foreach (var type in result.Types)
        {
            if (string.IsNullOrEmpty(type.Namespace)) continue;

            foreach (var refNs in type.ReferencedNamespaces)
            {
                if (internalNamespaces.Contains(refNs) && refNs != type.Namespace)
                    edges.Add((type.Namespace, refNs));
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("graph TD");

        if (!edges.Any())
        {
            sb.AppendLine("    %% No internal namespace dependencies found");
            return sb.ToString();
        }

        foreach (var (from, to) in edges.OrderBy(e => e.From).ThenBy(e => e.To))
        {
            var nodeFrom = ToMermaidId(from);
            var nodeTo = ToMermaidId(to);
            // Use display labels with the original namespace names
            sb.AppendLine($"    {nodeFrom}[\"{from}\"] --> {nodeTo}[\"{to}\"]");
        }

        return sb.ToString();
    }

    private static string ToMermaidId(string namespaceName) =>
        namespaceName.Replace('.', '_').Replace(' ', '_').Replace('-', '_');
}
