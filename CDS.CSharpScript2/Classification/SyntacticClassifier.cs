using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CDS.CSharpScript2.Classification;

/// <summary>
/// Classifies a script from its syntax tree alone — no compilation, no semantic model.
/// </summary>
/// <remarks>
/// This is the fast tier of a two-tier classification scheme. It produces roughly 95% of the
/// spans Roslyn's own classifier does, for a fraction of the cost (measured 3–16× cheaper
/// depending on script size), because everything it needs is already in the parse tree.
/// What it cannot know is anything requiring symbol resolution: whether an identifier is a
/// class, a method, a parameter or a local. Those all come back as
/// <see cref="SymbolClassification.Identifier"/> and are refined when the semantic pass
/// lands.
/// <para>
/// Roslyn exposes no public syntactic-only classification API — <c>Classifier</c> requires a
/// <see cref="Document"/> or <see cref="SemanticModel"/>, and <c>ISyntaxClassificationService</c>
/// is internal — so this walk is written by hand rather than delegated.
/// </para>
/// </remarks>
public static class SyntacticClassifier
{
    /// <summary>
    /// Returns syntactic classifications for the whole tree.
    /// </summary>
    /// <param name="syntaxTree">The tree to classify.</param>
    /// <param name="cancellationToken">A token that abandons the walk.</param>
    /// <returns>Classified spans ordered by position.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="syntaxTree"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ClassifiedSymbol> Classify(
        SyntaxTree syntaxTree,
        CancellationToken cancellationToken)
    {
        if (syntaxTree is null) { throw new ArgumentNullException(nameof(syntaxTree)); }

        var root = syntaxTree.GetRoot(cancellationToken);
        var results = new List<ClassifiedSymbol>();

        // descendIntoTrivia reaches the tokens inside preprocessor directives and XML doc
        // comments, which would otherwise be invisible to a plain token walk.
        foreach (var token in root.DescendantTokens(descendIntoTrivia: true))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (token.Span.IsEmpty) { continue; }

            if (ClassifyToken(token) is { } classification)
            {
                results.Add(new ClassifiedSymbol(token.Span.Start, token.Span.Length, classification));
            }
        }

        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (trivia.Span.IsEmpty) { continue; }

            if (ClassifyTrivia(trivia) is { } classification)
            {
                results.Add(new ClassifiedSymbol(trivia.Span.Start, trivia.Span.Length, classification));
            }
        }

        // The editor applies spans in order, so later spans win where they overlap. Sorting by
        // position keeps that deterministic.
        results.Sort(static (a, b) => a.SpanStart.CompareTo(b.SpanStart));

        return results;
    }

    /// <summary>
    /// Maps a token to its classification, or <see langword="null"/> when it should be left unstyled.
    /// </summary>
    private static SymbolClassification? ClassifyToken(SyntaxToken token)
    {
        var kind = token.Kind();

        switch (kind)
        {
            case SyntaxKind.IdentifierToken:
                // "var" is a contextual keyword, so the parser hands it back as an ordinary
                // identifier token. Colour it as a keyword the way every C# editor does.
                return IsImplicitTypeKeyword(token)
                    ? SymbolClassification.Keyword
                    : SymbolClassification.Identifier;

            case SyntaxKind.NumericLiteralToken:
                return SymbolClassification.NumericLiteral;

            case SyntaxKind.CharacterLiteralToken:
                return SymbolClassification.StringLiteral;

            case SyntaxKind.StringLiteralToken:
                // Verbatim strings get their own style; the tree does not distinguish them by
                // kind, so the leading @ is the only signal available here.
                return token.Text.StartsWith("@", StringComparison.Ordinal)
                    ? SymbolClassification.VerbatimStringLiteral
                    : SymbolClassification.StringLiteral;

            case SyntaxKind.InterpolatedStringTextToken:
                return SymbolClassification.StringLiteral;

            case SyntaxKind.XmlTextLiteralToken:
            case SyntaxKind.XmlTextLiteralNewLineToken:
            case SyntaxKind.XmlEntityLiteralToken:
                return SymbolClassification.XmlDocCommentText;

            case SyntaxKind.EndOfFileToken:
            case SyntaxKind.EndOfDirectiveToken:
                return null;
        }

        // Directive keywords share their SyntaxKind with ordinary language keywords — #if and
        // if are both IfKeyword — so the kind alone cannot tell them apart. Only a token that
        // actually sits inside a directive is a preprocessor keyword.
        if (SyntaxFacts.IsPreprocessorKeyword(kind) && token.Parent is DirectiveTriviaSyntax)
        {
            return SymbolClassification.PreprocessorKeyword;
        }

        if (SyntaxFacts.IsKeywordKind(kind))
        {
            return IsControlKeyword(token, kind)
                ? SymbolClassification.ControlKeyword
                : SymbolClassification.Keyword;
        }

        if (SyntaxFacts.IsPunctuation(kind))
        {
            return IsStructuralPunctuation(kind)
                ? SymbolClassification.Punctuation
                : SymbolClassification.Operator;
        }

        return null;
    }

    /// <summary>
    /// Maps trivia to its classification, or <see langword="null"/> when it carries no colour of
    /// its own.
    /// </summary>
    /// <remarks>
    /// Structured trivia — documentation comments and preprocessor directives — is skipped here
    /// because its inner tokens are classified individually by the token walk. Emitting the
    /// container span as well would paint over them.
    /// </remarks>
    private static SymbolClassification? ClassifyTrivia(SyntaxTrivia trivia) =>
        trivia.Kind() switch
        {
            SyntaxKind.SingleLineCommentTrivia or
            SyntaxKind.MultiLineCommentTrivia => SymbolClassification.Comment,

            SyntaxKind.DocumentationCommentExteriorTrivia => SymbolClassification.XmlDocCommentDelimiter,

            SyntaxKind.DisabledTextTrivia => SymbolClassification.ExcludedCode,

            _ => null,
        };

    /// <summary>
    /// Returns <see langword="true"/> when an identifier token is the contextual keyword
    /// <c>var</c> standing in for a declared type.
    /// </summary>
    /// <remarks>
    /// Checking the surrounding declaration rather than just the text keeps a variable that
    /// happens to be named <c>var</c> from being coloured as a keyword.
    /// </remarks>
    private static bool IsImplicitTypeKeyword(SyntaxToken token)
    {
        if (token.Parent is not IdentifierNameSyntax { IsVar: true } name)
        {
            return false;
        }

        return name.Parent switch
        {
            VariableDeclarationSyntax declaration => declaration.Type == name,
            ForEachStatementSyntax forEach => forEach.Type == name,
            DeclarationExpressionSyntax declarationExpression => declarationExpression.Type == name,
            _ => false,
        };
    }

    /// <summary>
    /// Returns <see langword="true"/> for keywords that direct control flow, which are styled
    /// distinctly from ordinary keywords.
    /// </summary>
    private static bool IsControlKeyword(SyntaxToken token, SyntaxKind kind) =>
        kind switch
        {
            SyntaxKind.IfKeyword or
            SyntaxKind.ElseKeyword or
            SyntaxKind.SwitchKeyword or
            SyntaxKind.CaseKeyword or
            SyntaxKind.WhileKeyword or
            SyntaxKind.DoKeyword or
            SyntaxKind.ForKeyword or
            SyntaxKind.ForEachKeyword or
            SyntaxKind.BreakKeyword or
            SyntaxKind.ContinueKeyword or
            SyntaxKind.GotoKeyword or
            SyntaxKind.ReturnKeyword or
            SyntaxKind.YieldKeyword or
            SyntaxKind.ThrowKeyword or
            SyntaxKind.TryKeyword or
            SyntaxKind.CatchKeyword or
            SyntaxKind.FinallyKeyword or
            SyntaxKind.WhenKeyword or
            SyntaxKind.AwaitKeyword => true,

            // "default" is a control keyword only as a switch label; elsewhere it is the
            // default-value expression and stays an ordinary keyword.
            SyntaxKind.DefaultKeyword => token.Parent.IsKind(SyntaxKind.DefaultSwitchLabel),

            // Likewise "in": control flow in a foreach, otherwise a parameter modifier.
            SyntaxKind.InKeyword => token.Parent.IsKind(SyntaxKind.ForEachStatement),

            _ => false,
        };

    /// <summary>
    /// Distinguishes punctuation that structures code (braces, separators) from operators.
    /// </summary>
    private static bool IsStructuralPunctuation(SyntaxKind kind) =>
        kind is SyntaxKind.OpenBraceToken
            or SyntaxKind.CloseBraceToken
            or SyntaxKind.OpenParenToken
            or SyntaxKind.CloseParenToken
            or SyntaxKind.OpenBracketToken
            or SyntaxKind.CloseBracketToken
            or SyntaxKind.SemicolonToken
            or SyntaxKind.CommaToken
            or SyntaxKind.DotToken
            or SyntaxKind.ColonToken
            or SyntaxKind.HashToken;
}
