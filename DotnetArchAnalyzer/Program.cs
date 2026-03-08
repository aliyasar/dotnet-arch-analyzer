using System.CommandLine;
using DotnetArchAnalyzer.Analyzers;
using DotnetArchAnalyzer.CLI.Commands;
using DotnetArchAnalyzer.Core.Rules;
using DotnetArchAnalyzer.Parsing;
using DotnetArchAnalyzer.Reporting;
using DotnetArchAnalyzer.Rules;
using DotnetArchAnalyzer.Rules.Complexity;
using DotnetArchAnalyzer.Rules.Style;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSingleton<RoslynParser>();
services.AddSingleton<ConsoleReporter>();
services.AddSingleton<MermaidReporter>();

// arch/ rules
services.AddSingleton<IArchRule, LayerViolationRule>();
services.AddSingleton<IArchRule, CircularDependencyRule>();
services.AddSingleton<IArchRule, HighCouplingRule>();

// style/ rules
services.AddSingleton<IArchRule, InterfacePrefixRule>();
services.AddSingleton<IArchRule, AsyncSuffixRule>();
services.AddSingleton<IArchRule, NoEmptyCatchRule>();
services.AddSingleton<IArchRule, NamespaceMatchRule>();

// complexity/ rules
services.AddSingleton<IArchRule, MethodLengthRule>();
services.AddSingleton<IArchRule, ClassLengthRule>();
services.AddSingleton<IArchRule, ParameterCountRule>();
services.AddSingleton<IArchRule, CyclomaticComplexityRule>();
services.AddSingleton<IArchRule, NestingDepthRule>();

services.AddSingleton<JsonReporter>();
services.AddSingleton<ArchitectureAnalyzer>();
services.AddSingleton<AnalyzeCommand>();
services.AddSingleton<GraphCommand>();
services.AddSingleton<InitCommand>();
services.AddSingleton<RulesCommand>();

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
var analyzeFormat = new Option<string>(
    "--format",
    getDefaultValue: () => "console",
    description: "Output format: \"console\" (default) or \"json\"");
var analyzeOutput = new Option<string?>(
    "--output",
    getDefaultValue: () => null,
    description: "Output file path for --format json. Defaults to stdout.");
var analyzeMaxWarnings = new Option<int>(
    "--max-warnings",
    getDefaultValue: () => -1,
    description: "Fail (exit 1) if warnings exceed this number. Use 0 to treat all warnings as errors.");

analyzeCmd.AddArgument(analyzeArg);
analyzeCmd.AddOption(analyzeLegacy);
analyzeCmd.AddOption(analyzeFormat);
analyzeCmd.AddOption(analyzeOutput);
analyzeCmd.AddOption(analyzeMaxWarnings);
analyzeCmd.SetHandler((argPath, optPath, format, output, maxWarnings) =>
{
    var cmd = provider.GetRequiredService<AnalyzeCommand>();
    Environment.ExitCode = cmd.Execute(optPath ?? argPath, format, output, maxWarnings);
}, analyzeArg, analyzeLegacy, analyzeFormat, analyzeOutput, analyzeMaxWarnings);

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

// ── rules ─────────────────────────────────────────────────────────────────────
var rulesCmd    = new Command("rules", "List all available rules with their current severity and thresholds");
var rulesArg    = PathArg("Path to .NET project directory (reads dotnetarch.json from there)");
var rulesLegacy = LegacyPathOpt();

rulesCmd.AddArgument(rulesArg);
rulesCmd.AddOption(rulesLegacy);
rulesCmd.SetHandler((argPath, optPath) =>
{
    var cmd = provider.GetRequiredService<RulesCommand>();
    cmd.Execute(optPath ?? argPath);
}, rulesArg, rulesLegacy);

rootCommand.AddCommand(analyzeCmd);
rootCommand.AddCommand(graphCmd);
rootCommand.AddCommand(initCmd);
rootCommand.AddCommand(rulesCmd);

await rootCommand.InvokeAsync(args);
return Environment.ExitCode;
