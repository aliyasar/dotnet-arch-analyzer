using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;

namespace DotnetArchAnalyzer.Core.Rules;

public interface IArchRule
{
    string RuleId { get; }
    IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types,
        IReadOnlyList<MethodInfo> methods,
        ArchConfig config);
}
