namespace DotnetArchAnalyzer.Core.Models;

/// <summary>
/// Represents a method extracted from a type declaration.
/// Used by style and complexity rules.
/// </summary>
public sealed record MethodInfo(
    string TypeFullName,
    string TypeName,
    string MethodName,
    string Namespace,
    string FilePath,
    int Line,
    int LineCount,
    int ParameterCount,
    bool IsAsync,
    int CyclomaticComplexity,
    int MaxNestingDepth,
    IReadOnlyList<int> EmptyCatchLines);
