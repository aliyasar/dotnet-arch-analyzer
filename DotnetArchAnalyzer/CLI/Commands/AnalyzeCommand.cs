using DotnetArchAnalyzer.Analyzers;

namespace DotnetArchAnalyzer.CLI.Commands
{
    public class AnalyzeCommand
    {
        private readonly ArchitectureAnalyzer _analyzer;

        public AnalyzeCommand(ArchitectureAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public void Execute(string path)
        {
            _analyzer.Analyze(path);
        }
    }
}
