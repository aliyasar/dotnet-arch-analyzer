namespace DotnetArchAnalyzer.Core.Models;

public sealed class AnalysisResult
{
    public required string ProjectPath { get; init; }
    public required IReadOnlyList<TypeInfo> Types { get; init; }
    public required IReadOnlyList<MethodInfo> Methods { get; init; }
    public required IReadOnlyList<AnalysisViolation> Violations { get; init; }

    /// <summary>Path to dotnetarch.json that was used. Null = built-in defaults.</summary>
    public string? ConfigPath { get; init; }

    public int ErrorCount => Violations.Count(v => v.Severity == ViolationSeverity.Error);
    public int WarningCount => Violations.Count(v => v.Severity == ViolationSeverity.Warning);

    /// <summary>0–100 health score. Errors cost 10 pts, warnings cost 3 pts.</summary>
    public int Score => Math.Max(0, 100 - ErrorCount * 10 - WarningCount * 3);

    public string Grade => Score switch
    {
        >= 90 => "A",
        >= 75 => "B",
        >= 60 => "C",
        >= 40 => "D",
        _     => "F",
    };
}
