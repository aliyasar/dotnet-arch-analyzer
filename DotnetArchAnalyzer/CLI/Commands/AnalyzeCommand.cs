using DotnetArchAnalyzer.Analyzers;
using DotnetArchAnalyzer.Reporting;
using Spectre.Console;

namespace DotnetArchAnalyzer.CLI.Commands;

public sealed class AnalyzeCommand
{
    private readonly ArchitectureAnalyzer _analyzer;
    private readonly ConsoleReporter _reporter;

    public AnalyzeCommand(ArchitectureAnalyzer analyzer, ConsoleReporter reporter)
    {
        _analyzer = analyzer;
        _reporter = reporter;
    }

    public int Execute(string path)
    {
        Core.Models.AnalysisResult? result = null;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start($"Analyzing [bold]{Markup.Escape(path)}[/]...", _ =>
            {
                result = _analyzer.Analyze(path);
            });

        _reporter.Report(result!);
        return result!.ErrorCount > 0 ? 1 : 0;
    }
}
