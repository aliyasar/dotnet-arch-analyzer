using DotnetArchAnalyzer.Analyzers;
using DotnetArchAnalyzer.Reporting;
using Spectre.Console;

namespace DotnetArchAnalyzer.CLI.Commands;

public sealed class GraphCommand
{
    private readonly ArchitectureAnalyzer _analyzer;
    private readonly MermaidReporter _reporter;

    public GraphCommand(ArchitectureAnalyzer analyzer, MermaidReporter reporter)
    {
        _analyzer = analyzer;
        _reporter = reporter;
    }

    public void Execute(string path, string? outputFile)
    {
        Core.Models.AnalysisResult? result = null;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start($"Building dependency graph for [bold]{Markup.Escape(path)}[/]...", _ =>
            {
                result = _analyzer.Analyze(path);
            });

        var mermaid = _reporter.Generate(result!);

        if (outputFile != null)
        {
            File.WriteAllText(outputFile, mermaid);
            AnsiConsole.MarkupLine($"[green]Graph written to {Markup.Escape(outputFile)}[/]");
        }
        else
        {
            Console.Write(mermaid);
        }
    }
}
