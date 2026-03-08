using DotnetArchAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeInfo = DotnetArchAnalyzer.Core.Models.TypeInfo;
using TypeKind = DotnetArchAnalyzer.Core.Models.TypeKind;

namespace DotnetArchAnalyzer.Parsing;

public sealed class RoslynParser
{
    public (IReadOnlyList<TypeInfo> Types, IReadOnlyList<MethodInfo> Methods) Parse(string projectPath)
    {
        if (!Directory.Exists(projectPath))
            throw new DirectoryNotFoundException($"Project path not found: {projectPath}");

        var csFiles = GetCSharpFiles(projectPath);
        var types   = new List<TypeInfo>();
        var methods = new List<MethodInfo>();

        foreach (var file in csFiles)
            ParseFile(file, types, methods);

        return (types, methods);
    }

    private static void ParseFile(string filePath, List<TypeInfo> types, List<MethodInfo> methods)
    {
        string source;
        try { source = File.ReadAllText(filePath); }
        catch { return; }

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
                ClassDeclarationSyntax     => TypeKind.Class,
                InterfaceDeclarationSyntax => TypeKind.Interface,
                RecordDeclarationSyntax    => TypeKind.Record,
                StructDeclarationSyntax    => TypeKind.Struct,
                _                          => TypeKind.Other
            };

            var typeSpan  = tree.GetLineSpan(typeDecl.Span);
            var startLine = typeSpan.StartLinePosition.Line + 1;
            var lineCount = typeSpan.EndLinePosition.Line - typeSpan.StartLinePosition.Line + 1;
            var fullName  = string.IsNullOrEmpty(ns)
                ? typeDecl.Identifier.Text
                : $"{ns}.{typeDecl.Identifier.Text}";

            types.Add(new TypeInfo(
                FullName:             fullName,
                Namespace:            ns,
                Name:                 typeDecl.Identifier.Text,
                FilePath:             filePath,
                Kind:                 kind,
                ReferencedNamespaces: allUsings,
                Line:                 startLine,
                LineCount:            lineCount));

            // Extract methods declared directly in this type (not nested types)
            foreach (var methodDecl in typeDecl.Members.OfType<MethodDeclarationSyntax>())
            {
                var mSpan  = tree.GetLineSpan(methodDecl.Span);
                var mLine  = mSpan.StartLinePosition.Line + 1;
                var mCount = mSpan.EndLinePosition.Line - mSpan.StartLinePosition.Line + 1;

                var emptyCatchLines = methodDecl.DescendantNodes()
                    .OfType<CatchClauseSyntax>()
                    .Where(c => c.Block.Statements.Count == 0)
                    .Select(c => tree.GetLineSpan(c.Span).StartLinePosition.Line + 1)
                    .ToList();

                methods.Add(new MethodInfo(
                    TypeFullName:         fullName,
                    TypeName:             typeDecl.Identifier.Text,
                    MethodName:           methodDecl.Identifier.Text,
                    Namespace:            ns,
                    FilePath:             filePath,
                    Line:                 mLine,
                    LineCount:            mCount,
                    ParameterCount:       methodDecl.ParameterList.Parameters.Count,
                    IsAsync:              methodDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)),
                    CyclomaticComplexity: ComputeCyclomatic(methodDecl),
                    MaxNestingDepth:      ComputeMaxNesting(methodDecl),
                    EmptyCatchLines:      emptyCatchLines));
            }
        }
    }

    /// <summary>
    /// McCabe cyclomatic complexity: 1 + number of decision points.
    /// Counts: if, while, for, foreach, case, catch, ternary (?:), logical && and ||.
    /// </summary>
    private static int ComputeCyclomatic(MethodDeclarationSyntax method)
    {
        int count = 1;
        foreach (var node in method.DescendantNodes())
        {
            count += node switch
            {
                IfStatementSyntax            => 1,
                WhileStatementSyntax         => 1,
                ForStatementSyntax           => 1,
                ForEachStatementSyntax       => 1,
                CaseSwitchLabelSyntax        => 1,
                CasePatternSwitchLabelSyntax => 1,
                CatchClauseSyntax            => 1,
                ConditionalExpressionSyntax  => 1,
                BinaryExpressionSyntax b when b.IsKind(SyntaxKind.LogicalAndExpression) => 1,
                BinaryExpressionSyntax b when b.IsKind(SyntaxKind.LogicalOrExpression)  => 1,
                _ => 0
            };
        }
        return count;
    }

    /// <summary>
    /// Returns the maximum nesting depth of control flow structures
    /// (if, while, for, foreach, try, switch) inside the method body.
    /// </summary>
    private static int ComputeMaxNesting(MethodDeclarationSyntax method)
    {
        var body = (SyntaxNode?)method.Body ?? method.ExpressionBody;
        return body is null ? 0 : GetMaxDepth(body, 0);
    }

    private static int GetMaxDepth(SyntaxNode node, int depth)
    {
        int max = depth;
        foreach (var child in node.ChildNodes())
        {
            var childDepth = child is IfStatementSyntax
                          || child is WhileStatementSyntax
                          || child is ForStatementSyntax
                          || child is ForEachStatementSyntax
                          || child is TryStatementSyntax
                          || child is SwitchStatementSyntax
                ? depth + 1
                : depth;

            max = Math.Max(max, GetMaxDepth(child, childDepth));
        }
        return max;
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
