namespace DotnetArchAnalyzer.Core.Models;

public sealed class AnalysisResult
{
    public required string ProjectPath { get; init; }
    public required IReadOnlyList<TypeInfo> Types { get; init; }
    public required IReadOnlyList<AnalysisViolation> Violations { get; init; }

    /// <summary>Path to dotnetarch.json that was used. Null = built-in defaults.</summary>
    public string? ConfigPath { get; init; }

    public int ErrorCount => Violations.Count(v => v.Severity == ViolationSeverity.Error);
    public int WarningCount => Violations.Count(v => v.Severity == ViolationSeverity.Warning);
}
