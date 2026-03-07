using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;
using DotnetArchAnalyzer.Parsing;

namespace DotnetArchAnalyzer.Analyzers;

public sealed class ArchitectureAnalyzer
{
    private readonly RoslynParser _parser;
    private readonly IEnumerable<IArchRule> _rules;

    public ArchitectureAnalyzer(RoslynParser parser, IEnumerable<IArchRule> rules)
    {
        _parser = parser;
        _rules = rules;
    }

    public AnalysisResult Analyze(string projectPath)
    {
        var absolutePath = Path.GetFullPath(projectPath);
        var (config, configPath) = ArchConfigLoader.Load(absolutePath);
        var types = _parser.Parse(absolutePath);

        var violations = _rules
            .SelectMany(rule => rule.Analyze(types, config))
            .ToList();

        return new AnalysisResult
        {
            ProjectPath = absolutePath,
            Types = types,
            Violations = violations,
            ConfigPath = configPath,
        };
    }
}
