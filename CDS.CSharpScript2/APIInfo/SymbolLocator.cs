using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Locates the relevant syntax node and token for a given position in a syntax tree.
/// </summary>
public static class SymbolLocator
{
    public class SymbolContext
    {
        public required SyntaxToken Token { get; set; }
        public required SyntaxNode Node { get; set; }
        public required SyntaxTree SyntaxTree { get; set; }
    }

    public static SymbolContext? Locate(SyntaxTree? syntaxTree, int position)
    {
        if (syntaxTree == null)
        {
            throw new ArgumentNullException(nameof(syntaxTree));
        }

        var root = syntaxTree.GetRoot();
        var token = root.FindToken(position, findInsideTrivia: true);
        if (SyntaxFacts.IsTrivia((SyntaxKind)token.RawKind))
        {
            token = token.GetPreviousToken();
        }
        if (!token.Span.IntersectsWith(position) && position == 0)
        {
            token = root.GetFirstToken();
        }
        if (token == default || token.IsKind(SyntaxKind.None) || (token.IsKind(SyntaxKind.EndOfFileToken) && token.FullSpan.Length == 0))
        {
            return null;
        }
        var node = token.Parent;
        if (node == null)
        {
            return null;
        }
        return new SymbolContext
        {
            Token = token,
            Node = node,
            SyntaxTree = syntaxTree
        };
    }
}
