using DotnetArchAnalyzer.Parsers;

namespace DotnetArchAnalyzer.Analyzers
{
    public sealed class ArchitectureAnalyzer
    {
        private readonly CSharpProjectParser _parser;

        public ArchitectureAnalyzer(CSharpProjectParser parser)
        {
            _parser = parser;
        }

        public void Analyze(string projectPath)
        {
            Console.WriteLine($"Analyzing project: {projectPath}");

            var files = _parser.GetCSharpFiles(projectPath);

            Console.WriteLine($"Found {files.Count} C# files");
        }
    }
}