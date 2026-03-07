namespace DotnetArchAnalyzer.Core.Models;

public sealed record AnalysisViolation(
    ViolationSeverity Severity,
    string RuleId,
    string Message,
    string? FilePath = null,
    int? Line = null);
