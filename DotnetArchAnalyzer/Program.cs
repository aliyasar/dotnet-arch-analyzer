using System.CommandLine;
using DotnetArchAnalyzer.Analyzers;
using DotnetArchAnalyzer.CLI.Commands;
using DotnetArchAnalyzer.Core.Rules;
using DotnetArchAnalyzer.Parsing;
using DotnetArchAnalyzer.Reporting;
using DotnetArchAnalyzer.Rules;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSingleton<RoslynParser>();
services.AddSingleton<ConsoleReporter>();
services.AddSingleton<MermaidReporter>();
services.AddSingleton<IArchRule, LayerViolationRule>();
services.AddSingleton<IArchRule, CircularDependencyRule>();
services.AddSingleton<IArchRule, HighCouplingRule>();
services.AddSingleton<ArchitectureAnalyzer>();
services.AddSingleton<AnalyzeCommand>();
services.AddSingleton<GraphCommand>();
services.AddSingleton<InitCommand>();

var provider = services.BuildServiceProvider();

var rootCommand = new RootCommand("dotnet-arch — .NET architecture analyzer");

// Hidden --path option kept for backward compatibility (prefer positional arg).
static Option<string?> LegacyPathOpt() =>
    new Option<string?>("--path", description: "Path to .NET project directory (deprecated, use positional argument)")
    {
        IsHidden = true
    };

static Argument<string> PathArg(string description) =>
    new("path", getDefaultValue: () => ".", description: description);

// ── analyze ──────────────────────────────────────────────────────────────────
var analyzeCmd    = new Command("analyze", "Analyze architecture and report violations");
var analyzeArg    = PathArg("Path to .NET project directory (default: current directory)");
var analyzeLegacy = LegacyPathOpt();

analyzeCmd.AddArgument(analyzeArg);
analyzeCmd.AddOption(analyzeLegacy);
analyzeCmd.SetHandler((argPath, optPath) =>
{
    var cmd = provider.GetRequiredService<AnalyzeCommand>();
    Environment.ExitCode = cmd.Execute(optPath ?? argPath);
}, analyzeArg, analyzeLegacy);

// ── graph ─────────────────────────────────────────────────────────────────────
var graphCmd    = new Command("graph", "Generate Mermaid dependency graph");
var graphArg    = PathArg("Path to .NET project directory (default: current directory)");
var graphLegacy = LegacyPathOpt();
var graphOutput = new Option<string?>(
    "--output",
    getDefaultValue: () => null,
    description: "Output file path (.md recommended). Defaults to stdout.");

graphCmd.AddArgument(graphArg);
graphCmd.AddOption(graphLegacy);
graphCmd.AddOption(graphOutput);
graphCmd.SetHandler((argPath, optPath, output) =>
{
    var cmd = provider.GetRequiredService<GraphCommand>();
    cmd.Execute(optPath ?? argPath, output);
}, graphArg, graphLegacy, graphOutput);

// ── init ──────────────────────────────────────────────────────────────────────
var initCmd    = new Command("init", "Create a dotnetarch.json config file with defaults");
var initArg    = PathArg("Directory where dotnetarch.json will be created (default: current directory)");
var initLegacy = LegacyPathOpt();

initCmd.AddArgument(initArg);
initCmd.AddOption(initLegacy);
initCmd.SetHandler((argPath, optPath) =>
{
    var cmd = provider.GetRequiredService<InitCommand>();
    cmd.Execute(optPath ?? argPath);
}, initArg, initLegacy);

rootCommand.AddCommand(analyzeCmd);
rootCommand.AddCommand(graphCmd);
rootCommand.AddCommand(initCmd);

return await rootCommand.InvokeAsync(args);
