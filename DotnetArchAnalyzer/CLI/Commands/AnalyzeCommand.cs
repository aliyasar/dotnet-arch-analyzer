using DotnetArchAnalyzer.Analyzers;
using DotnetArchAnalyzer.Reporting;
using Spectre.Console;

namespace DotnetArchAnalyzer.CLI.Commands;

public sealed class AnalyzeCommand
{
    private readonly ArchitectureAnalyzer _analyzer;
    private readonly ConsoleReporter _consoleReporter;
    private readonly JsonReporter _jsonReporter;

    public AnalyzeCommand(
        ArchitectureAnalyzer analyzer,
        ConsoleReporter consoleReporter,
        JsonReporter jsonReporter)
    {
        _analyzer        = analyzer;
        _consoleReporter = consoleReporter;
        _jsonReporter    = jsonReporter;
    }

    /// <param name="path">Project directory to analyze.</param>
    /// <param name="format">"console" (default) or "json".</param>
    /// <param name="output">Optional output file (used with --format json).</param>
    /// <param name="maxWarnings">Fail if warnings exceed this number. -1 = unlimited.</param>
    public int Execute(string path, string format = "console", string? output = null, int maxWarnings = -1)
    {
        Core.Models.AnalysisResult? result = null;

        if (format == "json")
        {
            result = _analyzer.Analyze(path);
            _jsonReporter.Report(result, output);
        }
        else
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start($"Analyzing [bold]{Markup.Escape(path)}[/]...", _ =>
                {
                    result = _analyzer.Analyze(path);
                });

            _consoleReporter.Report(result!);
        }

        if (result!.ErrorCount > 0) return 1;
        if (maxWarnings >= 0 && result.WarningCount > maxWarnings) return 1;
        return 0;
    }
}
