using DotnetArchAnalyzer.Analyzers;
using DotnetArchAnalyzer.CLI;
using DotnetArchAnalyzer.CLI.Commands;
using DotnetArchAnalyzer.Parsers;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSingleton<CommandHandler>();
services.AddSingleton<ArchitectureAnalyzer>();
services.AddSingleton<CSharpProjectParser>();

services.AddSingleton<AnalyzeCommand>();
services.AddSingleton<ReportCommand>();
services.AddSingleton<GraphCommand>();

var provider = services.BuildServiceProvider();

var handler = provider.GetRequiredService<CommandHandler>();

handler.Run(args);