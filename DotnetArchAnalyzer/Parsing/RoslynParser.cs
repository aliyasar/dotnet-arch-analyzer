using DotnetArchAnalyzer.Core.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetArchAnalyzer.Parsing;

public sealed class RoslynParser
{
    public IReadOnlyList<TypeInfo> Parse(string projectPath)
    {
        if (!Directory.Exists(projectPath))
            throw new DirectoryNotFoundException($"Project path not found: {projectPath}");

        var csFiles = GetCSharpFiles(projectPath);
        var result = new List<TypeInfo>();

        foreach (var file in csFiles)
            result.AddRange(ParseFile(file));

        return result;
    }

    private static IEnumerable<TypeInfo> ParseFile(string filePath)
    {
        string source;
        try { source = File.ReadAllText(filePath); }
        catch { yield break; }

        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        // File-level using directives
        var fileUsings = root.Usings
            .Select(u => u.Name?.ToString())
            .Where(n => n != null)
            .Cast<string>()
            .ToList();

        foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            // Usings inside enclosing namespace blocks
            var nsUsings = typeDecl.Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .SelectMany(ns => ns.Usings)
                .Select(u => u.Name?.ToString())
                .Where(n => n != null)
                .Cast<string>();

            var allUsings = fileUsings.Concat(nsUsings).Distinct().ToList();

            var ns = GetNamespace(typeDecl);

            var kind = typeDecl switch
            {
                ClassDeclarationSyntax => TypeKind.Class,
                InterfaceDeclarationSyntax => TypeKind.Interface,
                RecordDeclarationSyntax => TypeKind.Record,
                StructDeclarationSyntax => TypeKind.Struct,
                _ => TypeKind.Other
            };

            var line = tree.GetLineSpan(typeDecl.Span).StartLinePosition.Line + 1;
            var fullName = string.IsNullOrEmpty(ns) ? typeDecl.Identifier.Text : $"{ns}.{typeDecl.Identifier.Text}";

            yield return new TypeInfo(
                FullName: fullName,
                Namespace: ns,
                Name: typeDecl.Identifier.Text,
                FilePath: filePath,
                Kind: kind,
                ReferencedNamespaces: allUsings,
                Line: line);
        }
    }

    private static string GetNamespace(TypeDeclarationSyntax typeDecl)
    {
        // C# 10+ file-scoped namespace
        var fileScoped = typeDecl.Ancestors()
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();
        if (fileScoped != null) return fileScoped.Name.ToString();

        // Traditional block-scoped namespace
        var blockScoped = typeDecl.Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();
        return blockScoped?.Name.ToString() ?? string.Empty;
    }

    private static IEnumerable<string> GetCSharpFiles(string projectPath)
    {
        var sep = Path.DirectorySeparatorChar;
        var ignoredSegments = LoadIgnorePatterns(projectPath);

        return Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                if (f.Contains($"{sep}bin{sep}") || f.Contains($"{sep}obj{sep}"))
                    return false;

                foreach (var pattern in ignoredSegments)
                {
                    if (f.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            });
    }

    /// <summary>
    /// Reads .archignore from the project root and returns normalized path segments to exclude.
    /// Lines starting with # are comments. Blank lines are ignored.
    /// </summary>
    private static List<string> LoadIgnorePatterns(string projectPath)
    {
        var sep = Path.DirectorySeparatorChar;
        var ignorePath = Path.Combine(projectPath, ".archignore");

        if (!File.Exists(ignorePath))
            return [];

        return File.ReadAllLines(ignorePath)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .Select(l => $"{sep}{l.Replace('/', sep).TrimEnd(sep)}{sep}")
            .ToList();
    }
}
