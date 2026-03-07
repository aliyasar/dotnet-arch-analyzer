using DotnetArchAnalyzer.Core.Config;
using Spectre.Console;

namespace DotnetArchAnalyzer.CLI.Commands;

public sealed class InitCommand
{
    private const string DefaultConfig = """
        {
          // dotnetarch.json — architecture rules for dotnet-arch
          //
          // Layer keywords are matched against namespace segments (case-insensitive).
          // Example: "MyApp.Services.UserService" matches on the "services" segment.
          //
          // Supported layer names: domain, application, infrastructure, presentation
          // Aliases also accepted: web, ui, api (→ presentation), data, persistence (→ infrastructure), core (→ domain)
          //
          // Customize to fit your project's naming conventions.
          "layers": {
            "domain":         ["models", "entities", "dto", "exceptions"],
            "application":    ["services", "handlers"],
            "infrastructure": ["repositories", "context", "dataaccess"],
            "web":            ["controllers"]
          },

          // Rule severity: "error" | "warning" | "info" | "off"
          "rules": {
            "arch/layer-violation":     "warning",
            "arch/circular-dependency": "error",
            "arch/high-coupling":       "warning"
          },

          // Numeric thresholds for rules that support them
          "thresholds": {
            "arch/high-coupling": 10
          }
        }
        """;

    public void Execute(string path)
    {
        var configPath = Path.Combine(Path.GetFullPath(path), ArchConfigLoader.ConfigFileNames[0]);

        // Also check legacy .archrc.json
        var legacyPath = Path.Combine(Path.GetFullPath(path), ArchConfigLoader.ConfigFileNames[1]);
        if (File.Exists(configPath) || File.Exists(legacyPath))
        {
            var existing = File.Exists(configPath) ? configPath : legacyPath;
            AnsiConsole.MarkupLine($"[yellow]Config already exists:[/] {existing}");
            return;
        }

        File.WriteAllText(configPath, DefaultConfig);
        AnsiConsole.MarkupLine($"[green]Created[/] {configPath}");
        AnsiConsole.MarkupLine("[grey]Edit the file to customize layer keywords and rule severities.[/]");
    }
}
