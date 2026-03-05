using DotnetArchAnalyzer.CLI.Commands;

namespace DotnetArchAnalyzer.CLI
{
    internal class CommandHandler
    {
        private readonly AnalyzeCommand _analyze;
        private readonly ReportCommand _report;
        private readonly GraphCommand _graph;

        public CommandHandler(AnalyzeCommand analyze, ReportCommand report, GraphCommand graph)
        {
            _analyze = analyze;
            _report = report;
            _graph = graph;
        }

        public void Run(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dotnet-arch <command>");
                return;
            }

            var command = args[0];
            var path = args.Length > 1 ? args[1] : ".";

            switch (command.ToLower())
            {
                case "analyze":
                    _analyze.Execute(path);
                    break;

                case "report":
                    _report.Execute(path);
                    break;

                case "graph":
                    _graph.Execute(path);
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }
    }
}
