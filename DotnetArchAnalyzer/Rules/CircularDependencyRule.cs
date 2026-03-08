using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules;

/// <summary>
/// Detects circular namespace dependencies using DFS cycle detection.
/// Only considers internal namespaces (those present in the parsed codebase).
/// </summary>
public sealed class CircularDependencyRule : IArchRule
{
    public string RuleId => "arch/circular-dependency";

    public IEnumerable<AnalysisViolation> Analyze(IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var internalNamespaces = types
            .Select(t => t.Namespace)
            .Where(ns => !string.IsNullOrEmpty(ns))
            .ToHashSet();

        // Build directed graph: namespace → set of namespaces it depends on
        var graph = new Dictionary<string, HashSet<string>>();
        foreach (var type in types)
        {
            if (string.IsNullOrEmpty(type.Namespace)) continue;

            if (!graph.ContainsKey(type.Namespace))
                graph[type.Namespace] = [];

            foreach (var dep in type.ReferencedNamespaces)
            {
                if (internalNamespaces.Contains(dep) && dep != type.Namespace)
                    graph[type.Namespace].Add(dep);
            }
        }

        var done = new HashSet<string>();
        var reportedCycles = new HashSet<string>();

        foreach (var node in graph.Keys)
        {
            if (!done.Contains(node))
                DetectCycles(node, graph, done, [], [], reportedCycles);
        }

        foreach (var cycleKey in reportedCycles)
        {
            yield return new AnalysisViolation(
                ViolationSeverity.Error,
                RuleId,
                $"Circular dependency: {cycleKey}");
        }
    }

    private static void DetectCycles(
        string node,
        Dictionary<string, HashSet<string>> graph,
        HashSet<string> done,
        HashSet<string> inStack,
        List<string> path,
        HashSet<string> reportedCycles)
    {
        inStack.Add(node);
        path.Add(node);

        if (graph.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (done.Contains(neighbor)) continue;

                if (inStack.Contains(neighbor))
                {
                    var cycleStart = path.IndexOf(neighbor);
                    var cycle = path.Skip(cycleStart).Append(neighbor).ToList();

                    // Normalize to avoid reporting the same cycle multiple times
                    var minItem = cycle.Take(cycle.Count - 1).Min()!;
                    var minIdx = cycle.IndexOf(minItem);
                    var normalized = cycle.Skip(minIdx).Take(cycle.Count - 1)
                        .Concat(cycle.Skip(minIdx).Take(1))
                        .ToList();

                    reportedCycles.Add(string.Join(" -> ", normalized));
                }
                else
                {
                    DetectCycles(neighbor, graph, done, inStack, path, reportedCycles);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        inStack.Remove(node);
        done.Add(node);
    }
}
