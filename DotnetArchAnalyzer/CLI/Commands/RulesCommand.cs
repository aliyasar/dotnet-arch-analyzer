using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Rules;
using Spectre.Console;

namespace DotnetArchAnalyzer.CLI.Commands;

public sealed class RulesCommand
{
    private readonly IEnumerable<IArchRule> _rules;

    public RulesCommand(IEnumerable<IArchRule> rules)
    {
        _rules = rules;
    }

    public void Execute(string path)
    {
        var absolutePath = Path.GetFullPath(path);
        var (config, configPath) = ArchConfigLoader.Load(absolutePath);

        AnsiConsole.WriteLine();

        if (configPath != null)
            AnsiConsole.MarkupLine($"[grey]Config: {Markup.Escape(configPath)}[/]");
        else
            AnsiConsole.MarkupLine("[grey]No dotnetarch.json found — using built-in defaults.[/]");

        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Rule[/]"))
            .AddColumn(new TableColumn("[bold]Severity[/]").Centered())
            .AddColumn(new TableColumn("[bold]Threshold[/]").RightAligned());

        foreach (var rule in _rules.OrderBy(r => r.RuleId))
        {
            var ruleId = rule.RuleId;

            string severityMarkup;
            if (!config.IsRuleEnabled(ruleId))
            {
                severityMarkup = "[grey]off[/]";
            }
            else
            {
                var sev = config.GetSeverityOverride(ruleId);
                severityMarkup = sev switch
                {
                    Core.Models.ViolationSeverity.Error   => "[red]error[/]",
                    Core.Models.ViolationSeverity.Warning => "[yellow]warning[/]",
                    Core.Models.ViolationSeverity.Info    => "[blue]info[/]",
                    _ => "[yellow]warning[/]"
                };
            }

            var threshold = config.Thresholds.TryGetValue(ruleId, out var t)
                ? t.ToString()
                : "[grey]—[/]";

            table.AddRow(Markup.Escape(ruleId), severityMarkup, threshold);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
