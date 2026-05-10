using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Locates the relevant syntax node and token for a given cursor position in a syntax tree.
/// </summary>
internal static class SymbolLocator
{
    internal sealed class SymbolContext
    {
        public required SyntaxToken Token { get; init; }
        public required SyntaxNode Node { get; init; }
        public required SyntaxTree SyntaxTree { get; init; }
    }

    internal static SymbolContext? Locate(SyntaxTree syntaxTree, int position)
    {
        var root = syntaxTree.GetRoot();
        var token = root.FindToken(position, findInsideTrivia: true);

        if (SyntaxFacts.IsTrivia((SyntaxKind)token.RawKind))
            token = token.GetPreviousToken();

        if (!token.Span.IntersectsWith(position) && position == 0)
            token = root.GetFirstToken();

        if (token == default
            || token.IsKind(SyntaxKind.None)
            || (token.IsKind(SyntaxKind.EndOfFileToken) && token.FullSpan.Length == 0))
            return null;

        var node = token.Parent;
        return node is null ? null : new SymbolContext { Token = token, Node = node, SyntaxTree = syntaxTree };
    }
}
