using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace CDS.CSharpScript2.Editors.Syntax;

public class ScriptSyntaxAnalyser
{
    /// <summary>
    /// Analyzes the syntax tree and returns a list of syntax elements for syntax highlighting.
    /// </summary>
    /// <param name="syntaxTree">The syntax tree to analyze.</param>
    /// <returns>An immutable array of syntax elements.</returns>
    public static ImmutableArray<SyntaxElement> Go(SyntaxTree syntaxTree)
    {
        var compilationUnitRoot = syntaxTree.GetCompilationUnitRoot();
        var syntaxElements = new List<SyntaxElement>();
        RecursiveAddNode(compilationUnitRoot, syntaxElements);
        RemoveDuplicates(syntaxElements);
        return syntaxElements.ToImmutableArray();
    }

    /// <summary>
    /// Removes duplicate syntax elements from the list.
    /// </summary>
    /// <param name="syntaxElements">The list of syntax elements.</param>
    private static void RemoveDuplicates(List<SyntaxElement> syntaxElements)
    {
        var bySpanStart = syntaxElements.OrderBy(s => s.Span.Start);
        var duplicateFree = new List<SyntaxElement>();

        foreach (var element in bySpanStart)
        {
            if (duplicateFree.Count == 0 || element != duplicateFree.Last())
            {
                duplicateFree.Add(element);
            }
        }

        syntaxElements.Clear();
        syntaxElements.AddRange(duplicateFree);
    }

    /// <summary>
    /// Recursively adds syntax nodes and their children to the list of syntax elements.
    /// </summary>
    /// <param name="syntaxNode">The syntax node to process.</param>
    /// <param name="syntaxElements">The list of syntax elements.</param>
    private static void RecursiveAddNode(SyntaxNode syntaxNode, List<SyntaxElement> syntaxElements)
    {
        syntaxElements.Add(new SyntaxElement(syntaxNode.Span, syntaxNode.Kind()));

        foreach (var leadingTrivia in syntaxNode.GetLeadingTrivia())
        {
            RecursiveAddTrivia(syntaxElements, leadingTrivia);
        }

        foreach (var childNode in syntaxNode.ChildNodesAndTokens())
        {
            if (childNode.IsNode)
            {
                RecursiveAddNode(childNode.AsNode(), syntaxElements);
            }
            else if (childNode.IsToken)
            {
                AddToken(childNode.AsToken(), syntaxElements);
            }
        }

        foreach (var trailingTrivia in syntaxNode.GetTrailingTrivia())
        {
            RecursiveAddTrivia(syntaxElements, trailingTrivia);
        }
    }

    /// <summary>
    /// Recursively adds syntax trivia to the list of syntax elements.
    /// </summary>
    /// <param name="syntaxElements">The list of syntax elements.</param>
    /// <param name="trivia">The syntax trivia to process.</param>
    private static void RecursiveAddTrivia(List<SyntaxElement> syntaxElements, SyntaxTrivia trivia)
    {
        var structure = trivia.GetStructure();
        if (structure != null)
        {
            RecursiveAddNode(structure, syntaxElements);
        }

        syntaxElements.Add(new SyntaxElement(trivia.Span, trivia.Kind()));
    }

    /// <summary>
    /// Adds a syntax token and its trivia to the list of syntax elements.
    /// </summary>
    /// <param name="syntaxToken">The syntax token to process.</param>
    /// <param name="syntaxElements">The list of syntax elements.</param>
    private static void AddToken(SyntaxToken syntaxToken, List<SyntaxElement> syntaxElements)
    {
        syntaxElements.Add(new SyntaxElement(syntaxToken.Span, syntaxToken.Kind()));

        foreach (var leadingTrivia in syntaxToken.LeadingTrivia)
        {
            RecursiveAddTrivia(syntaxElements, leadingTrivia);
        }

        foreach (var trailingTrivia in syntaxToken.TrailingTrivia)
        {
            RecursiveAddTrivia(syntaxElements, trailingTrivia);
        }
    }
}
