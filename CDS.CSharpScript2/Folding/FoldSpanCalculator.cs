using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CDS.CSharpScript2.Folding;

/// <summary>
/// Computes foldable ranges from a script's syntax tree — brace-delimited blocks and
/// <c>#region</c>/<c>#endregion</c> directive pairs.
/// </summary>
/// <remarks>
/// Matching braces and directives by hand rather than walking specific node types (blocks, type
/// declarations, initializers, switch statements, ...) catches every brace pair uniformly,
/// including ones new syntax forms would otherwise be missed by an allow-list.
/// </remarks>
public static class FoldSpanCalculator
{
    /// <summary>
    /// Returns foldable spans for the whole tree, ordered by start position.
    /// </summary>
    /// <param name="syntaxTree">The tree to scan.</param>
    /// <param name="cancellationToken">A token that abandons the walk.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="syntaxTree"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<FoldSpan> Calculate(SyntaxTree syntaxTree, CancellationToken cancellationToken)
    {
        if (syntaxTree is null) { throw new ArgumentNullException(nameof(syntaxTree)); }

        var root = syntaxTree.GetRoot(cancellationToken);
        var results = new List<FoldSpan>();

        // A lone unmatched brace or directive — typically mid-edit — is simply left unpaired on
        // its stack and never turned into a span, rather than treated as an error.
        var braceStarts = new Stack<int>();

        foreach (var token in root.DescendantTokens(descendIntoTrivia: true))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The '{' and '}' delimiting an interpolation hole (e.g. $"{expr}") are ordinary
            // OpenBraceToken/CloseBraceToken tokens, not trivia, so they'd otherwise be matched
            // as if they were a structural block.
            if (token.Parent.IsKind(SyntaxKind.Interpolation))
            {
                continue;
            }

            if (token.IsKind(SyntaxKind.OpenBraceToken))
            {
                braceStarts.Push(token.Span.Start);
            }
            else if (token.IsKind(SyntaxKind.CloseBraceToken) && braceStarts.Count > 0)
            {
                var start = braceStarts.Pop();
                results.Add(new FoldSpan(start, token.Span.End - start));
            }
        }

        var regionStarts = new Stack<int>();

        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
            {
                regionStarts.Push(trivia.FullSpan.Start);
            }
            else if (trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia) && regionStarts.Count > 0)
            {
                var start = regionStarts.Pop();
                results.Add(new FoldSpan(start, trivia.FullSpan.End - start));
            }
        }

        results.Sort(static (a, b) => a.SpanStart.CompareTo(b.SpanStart));

        return results;
    }
}
