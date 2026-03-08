namespace DotnetArchAnalyzer.Core.Models;

public enum TypeKind { Class, Interface, Record, Struct, Enum, Other }

public sealed record TypeInfo(
    string FullName,
    string Namespace,
    string Name,
    string FilePath,
    TypeKind Kind,
    IReadOnlyList<string> ReferencedNamespaces,
    int Line,
    int LineCount)
{
    /// <summary>
    /// Number of namespace-level dependencies (using directives).
    /// Use in rules like HighCouplingRule: type.DependencyCount > threshold.
    /// </summary>
    public int DependencyCount => ReferencedNamespaces.Count;
}
