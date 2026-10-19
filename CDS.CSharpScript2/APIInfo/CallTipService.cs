using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Locates the active argument position within a call or indexer expression at a given cursor position.
/// Uses the Roslyn syntax tree directly — no semantic analysis required.
/// </summary>
internal static class CallTipService
{
    /// <summary>
    /// Returns call tip context for the innermost argument list that contains
    /// <paramref name="position"/>, or <see langword="null"/> when the cursor is
    /// not inside any argument list.
    /// </summary>
    internal static CallTipContext? GetContext(SyntaxTree syntaxTree, int position)
    {
        var root = syntaxTree.GetRoot();
        var token = root.FindToken(position, findInsideTrivia: false);

        // At end-of-file the token is the EOF sentinel whose parent is the
        // compilation root, so the upward walk never finds an ArgumentListSyntax.
        // Step back to the real last token so the walk starts inside the tree.
        if (token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.EndOfFileToken))
            token = token.GetPreviousToken();

        if (token == default)
            return null;

        SyntaxNode? node = token.Parent;

        while (node is not null)
        {
            if (node is ArgumentListSyntax argList &&
                IsInside(argList.OpenParenToken, argList.CloseParenToken, argList.Span, position))
            {
                return new CallTipContext
                {
                    ArgumentIndex = GetArgumentIndex(argList.Arguments, position),
                    OpenParenPosition = argList.OpenParenToken.SpanStart,
                };
            }

            if (node is BracketedArgumentListSyntax bracketedArgList &&
                IsInside(bracketedArgList.OpenBracketToken, bracketedArgList.CloseBracketToken, bracketedArgList.Span, position))
            {
                return new CallTipContext
                {
                    ArgumentIndex = GetArgumentIndex(bracketedArgList.Arguments, position),
                    OpenParenPosition = bracketedArgList.OpenBracketToken.SpanStart,
                };
            }

            node = node.Parent;
        }

        return null;
    }

    /// <summary>
    /// True when <paramref name="position"/> lies between an argument list's brackets: after the
    /// opening one, and no further than the closing one — or, while that has not been typed yet,
    /// the end of the list. A caret just past a typed closing bracket has left the call, which is
    /// what lets an editor fall back to the enclosing call's tip.
    /// </summary>
    private static bool IsInside(SyntaxToken open, SyntaxToken close, Microsoft.CodeAnalysis.Text.TextSpan span, int position)
    {
        var end = close.IsMissing ? span.End : close.SpanStart;
        return open.SpanStart < position && position <= end;
    }

    /// <summary>
    /// Counts separating commas before <paramref name="position"/> to determine
    /// the zero-based argument index. Handles nested calls correctly because the
    /// separators inspected belong only to the innermost argument list.
    /// </summary>
    private static int GetArgumentIndex(SeparatedSyntaxList<ArgumentSyntax> arguments, int position)
    {
        int index = 0;

        for (int i = 0; i < arguments.SeparatorCount; i++)
        {
            if (arguments.GetSeparator(i).SpanStart < position)
                index++;
            else
                break;
        }

        return index;
    }
}
