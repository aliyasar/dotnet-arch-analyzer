using DotnetArchAnalyzer.Core.Models;
using Spectre.Console;

namespace DotnetArchAnalyzer.Reporting;

public sealed class ConsoleReporter
{
    public void Report(AnalysisResult result)
    {
        AnsiConsole.WriteLine();

        PrintHeader(result);

        if (!result.Violations.Any())
        {
            AnsiConsole.MarkupLine("[green]✓ No architecture violations found.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        PrintViolationsByRule(result);

        PrintRuleSummary(result);

        PrintSummaryLine(result);

        EmitGitHubAnnotations(result);
    }

    private static void PrintHeader(AnalysisResult result)
    {
        // Config source
        if (result.ConfigPath != null)
            AnsiConsole.MarkupLine($"[grey]Config: {Markup.Escape(result.ConfigPath)}[/]");
        else
            AnsiConsole.MarkupLine("[grey]No dotnetarch.json found — using built-in defaults. Run [italic]dotnet-arch init[/] to create one.[/]");

        AnsiConsole.WriteLine();

        var gradeColor = result.Grade switch
        {
            "A" => "green",
            "B" => "chartreuse2",
            "C" => "yellow",
            "D" => "darkorange",
            _   => "red"
        };

        AnsiConsole.MarkupLine(
            $"[bold white]Architecture Analysis[/]  " +
            $"[grey]Grade:[/] [{gradeColor} bold]{result.Grade}[/]  [grey]{result.Score}/100[/]");
        AnsiConsole.MarkupLine($"[grey]{new string('─', 64)}[/]");

        var depCount = CountInternalDependencies(result.Types);
        var violationColor = result.ErrorCount > 0 ? "red" : result.WarningCount > 0 ? "yellow" : "green";

        var table = new Table().NoBorder().HideHeaders();
        table.AddColumn(new TableColumn("").RightAligned());
        table.AddColumn(new TableColumn(""));
        table.AddRow("[grey]Types[/]",        $"{result.Types.Count}");
        table.AddRow("[grey]Methods[/]",      $"{result.Methods.Count}");
        table.AddRow("[grey]Dependencies[/]", $"{depCount}");
        table.AddRow("[grey]Violations[/]",   $"[{violationColor}]{result.Violations.Count}[/]");
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void PrintViolationsByRule(AnalysisResult result)
    {
        var byRule = result.Violations
            .GroupBy(v => v.RuleId)
            .OrderBy(g => g.Key);

        foreach (var ruleGroup in byRule)
        {
            var violations = ruleGroup.ToList();
            var haserror   = violations.Any(v => v.Severity == ViolationSeverity.Error);
            var color      = haserror ? "red" : "yellow";
            var severityLabel = haserror ? "error" : "warning";

            AnsiConsole.MarkupLine(
                $"[{color} bold]{Markup.Escape(ruleGroup.Key)}[/]  " +
                $"[grey]({violations.Count} {Plural(violations.Count, severityLabel)})[/]");
            AnsiConsole.MarkupLine($"[grey]{new string('─', 64)}[/]");

            // Group by file within each rule
            var byFile = violations
                .Where(v => v.FilePath != null)
                .GroupBy(v => v.FilePath!)
                .OrderBy(g => g.Key);

            foreach (var fileGroup in byFile)
            {
                var relative = MakeRelative(fileGroup.Key, result.ProjectPath);
                AnsiConsole.MarkupLine($"  [bold white]{Markup.Escape(relative)}[/]");

                foreach (var v in fileGroup.OrderBy(x => x.Line ?? 0))
                {
                    var icon    = v.Severity == ViolationSeverity.Error ? "✖" : "⚠";
                    var vcolor  = v.Severity == ViolationSeverity.Error ? "red" : "yellow";
                    var lineCol = v.Line.HasValue ? $"{v.Line,5}" : "     ";
                    AnsiConsole.MarkupLine(
                        $"  [grey]{lineCol}[/]  [{vcolor}]{icon}[/]  {Markup.Escape(v.Message)}");
                }

                AnsiConsole.WriteLine();
            }

            // Violations without a file (e.g. circular dependency)
            foreach (var v in violations.Where(v => v.FilePath == null))
            {
                var icon   = v.Severity == ViolationSeverity.Error ? "✖" : "⚠";
                var vcolor = v.Severity == ViolationSeverity.Error ? "red" : "yellow";
                AnsiConsole.MarkupLine($"  [{vcolor}]{icon}[/]  {Markup.Escape(v.Message)}");
                AnsiConsole.WriteLine();
            }
        }
    }

    private static void PrintRuleSummary(AnalysisResult result)
    {
        var byRule = result.Violations
            .GroupBy(v => v.RuleId)
            .OrderBy(g => g.Key)
            .ToList();

        if (!byRule.Any()) return;

        AnsiConsole.MarkupLine("[grey]Rule summary[/]");
        AnsiConsole.MarkupLine($"[grey]{new string('─', 64)}[/]");

        foreach (var group in byRule)
        {
            var errors   = group.Count(v => v.Severity == ViolationSeverity.Error);
            var warnings = group.Count(v => v.Severity == ViolationSeverity.Warning);

            var parts = new List<string>();
            if (errors   > 0) parts.Add($"[red]{errors} {Plural(errors, "error")}[/]");
            if (warnings > 0) parts.Add($"[yellow]{warnings} {Plural(warnings, "warning")}[/]");

            AnsiConsole.MarkupLine(
                $"  [grey]{Markup.Escape(group.Key),-32}[/]  {string.Join(", ", parts)}");
        }

        AnsiConsole.WriteLine();
    }

    private static void PrintSummaryLine(AnalysisResult result)
    {
        var total = result.Violations.Count;
        var color = result.ErrorCount > 0 ? "red" : "yellow";
        AnsiConsole.MarkupLine(
            $"[{color} bold]✖ {total} {Plural(total, "problem")} " +
            $"({result.ErrorCount} {Plural(result.ErrorCount, "error")}, " +
            $"{result.WarningCount} {Plural(result.WarningCount, "warning")})[/]");
    }

    /// <summary>Counts unique internal namespace-to-namespace dependency edges.</summary>
    private static int CountInternalDependencies(IReadOnlyList<TypeInfo> types)
    {
        var internalNs = types.Select(t => t.Namespace).Where(ns => !string.IsNullOrEmpty(ns)).ToHashSet();
        return types
            .SelectMany(t => t.ReferencedNamespaces
                .Where(r => r != t.Namespace && internalNs.Contains(r))
                .Select(r => (t.Namespace, r)))
            .Distinct()
            .Count();
    }

    private static string MakeRelative(string path, string basePath)
    {
        try { return Path.GetRelativePath(basePath, path); }
        catch { return path; }
    }

    private static string Plural(int count, string word) =>
        count == 1 ? word : $"{word}s";

    /// <summary>
    /// Emits GitHub Actions workflow commands so violations appear as inline annotations on PRs.
    /// Only runs when the GITHUB_ACTIONS environment variable is "true".
    /// Format: ::error file={file},line={line}::{message}
    /// </summary>
    private static void EmitGitHubAnnotations(AnalysisResult result)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true",
                StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var v in result.Violations)
        {
            var level = v.Severity == ViolationSeverity.Error ? "error" : "warning";
            var file  = v.FilePath != null
                ? MakeRelative(v.FilePath, result.ProjectPath)
                : string.Empty;

            var location = file.Length > 0
                ? (v.Line.HasValue ? $"file={file},line={v.Line}" : $"file={file}")
                : string.Empty;

            var prefix = location.Length > 0 ? $"::{level} {location}::" : $"::{level}::";

            // GitHub annotations must go to stdout without Spectre markup
            Console.WriteLine($"{prefix}[{v.RuleId}] {v.Message}");
        }
    }
}
