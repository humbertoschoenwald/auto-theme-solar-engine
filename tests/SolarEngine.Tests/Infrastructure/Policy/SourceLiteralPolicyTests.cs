// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace SolarEngine.Tests.Infrastructure.Policy;

/// <summary>
/// Guards the repository rule that authored product code must name behavioral literals.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class SourceLiteralPolicyTests
{
    private static readonly string s_repositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    /// <summary>
    /// Verifies authored product source keeps behavioral literals behind explicit names.
    /// </summary>
    [Fact]
    public void AuthoredSourceDoesNotUseMagicLiterals()
    {
        string[] violations =
        [
            .. EnumerateAuthoredProductSourceFiles()
                .SelectMany(FindViolations)
                .OrderBy(static violation => violation, StringComparer.Ordinal)
        ];

        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> FindViolations(string filePath)
    {
        SourceText sourceText = SourceText.From(File.ReadAllText(filePath));
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            sourceText,
            new CSharpParseOptions(LanguageVersion.Preview),
            filePath);
        SyntaxNode root = syntaxTree.GetRoot();

        foreach (SyntaxToken token in root.DescendantTokens())
        {
            if (!IsTrackedLiteral(token))
            {
                continue;
            }

            if (IsAllowedLiteral(token, filePath))
            {
                continue;
            }

            FileLinePositionSpan lineSpan = syntaxTree.GetLineSpan(token.Span);
            int lineNumber = lineSpan.StartLinePosition.Line + 1;
            int columnNumber = lineSpan.StartLinePosition.Character + 1;
            yield return $"{filePath}:{lineNumber}:{columnNumber}: {token.Text}";
        }
    }

    private static bool IsTrackedLiteral(SyntaxToken token)
    {
        return token.Kind() is SyntaxKind.NumericLiteralToken
            or SyntaxKind.StringLiteralToken
            or SyntaxKind.CharacterLiteralToken
            or SyntaxKind.Utf8StringLiteralToken;
    }

    private static bool IsAllowedLiteral(SyntaxToken token, string filePath)
    {
        return token.Parent is not null && (IsNamedDeclarationLiteral(token.Parent)
            || IsEnumLiteral(token.Parent)
            || IsMetadataLiteral(token.Parent) || IsDocumentedSolarNumericLiteral(token, filePath));
    }

    private static bool IsNamedDeclarationLiteral(SyntaxNode node)
    {
        return node.AncestorsAndSelf().Any(static ancestor =>
            (ancestor is VariableDeclaratorSyntax variableDeclarator
             && variableDeclarator.Parent?.Parent is BaseFieldDeclarationSyntax fieldDeclaration
             && fieldDeclaration.Declaration.Variables.Count == 1)
            || (ancestor is VariableDeclaratorSyntax localVariableDeclarator
                && localVariableDeclarator.Parent?.Parent is LocalDeclarationStatementSyntax localDeclaration
                && localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword)));
    }

    private static bool IsEnumLiteral(SyntaxNode node)
    {
        return node.AncestorsAndSelf().Any(static ancestor => ancestor is EnumMemberDeclarationSyntax);
    }

    private static bool IsMetadataLiteral(SyntaxNode node)
    {
        return node.AncestorsAndSelf().Any(static ancestor =>
            ancestor is AttributeSyntax
            or AttributeArgumentSyntax
            or AttributeListSyntax);
    }

    private static bool IsDocumentedSolarNumericLiteral(SyntaxToken token, string filePath)
    {
        return token.IsKind(SyntaxKind.NumericLiteralToken)
            && filePath.Contains(
                $"{Path.DirectorySeparatorChar}Features{Path.DirectorySeparatorChar}SolarCalculations{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateAuthoredProductSourceFiles()
    {
        string absoluteRoot = Path.Combine(s_repositoryRoot, "src");
        return !Directory.Exists(absoluteRoot)
            ? []
            : Directory.EnumerateFiles(absoluteRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static filePath =>
                !filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }
}
