using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetArchAnalyzer.Core.Models;

namespace DotnetArchAnalyzer.Reporting;

public sealed class JsonReporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public void Report(AnalysisResult result, string? outputPath = null)
    {
        var internalNs = result.Types
            .Select(t => t.Namespace)
            .Where(ns => !string.IsNullOrEmpty(ns))
            .ToHashSet();

        var depCount = result.Types
            .SelectMany(t => t.ReferencedNamespaces
                .Where(r => r != t.Namespace && internalNs.Contains(r))
                .Select(r => (t.Namespace, r)))
            .Distinct()
            .Count();

        var payload = new
        {
            projectPath = result.ProjectPath,
            configPath  = result.ConfigPath,
            summary = new
            {
                types        = result.Types.Count,
                methods      = result.Methods.Count,
                dependencies = depCount,
                errors       = result.ErrorCount,
                warnings     = result.WarningCount,
                violations   = result.Violations.Count,
            },
            violations = result.Violations.Select(v => new
            {
                ruleId   = v.RuleId,
                severity = v.Severity.ToString().ToLowerInvariant(),
                message  = v.Message,
                file     = v.FilePath != null
                    ? Path.GetRelativePath(result.ProjectPath, v.FilePath)
                    : null,
                line = v.Line,
            }),
        };

        var json = JsonSerializer.Serialize(payload, Options);

        if (outputPath != null)
        {
            File.WriteAllText(outputPath, json);
        }
        else
        {
            Console.WriteLine(json);
        }
    }
}
