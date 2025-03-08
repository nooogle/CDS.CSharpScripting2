using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CDS.CSScripting2.Editors.Syntax;

public class ScriptSyntaxAnalyser
{
    public static ImmutableArray<SyntaxElement> Go(SyntaxTree syntaxTree)
    {
        // This bit would replace everything apart from it fails to get the first 3 
        // backslashes of an XML documentation summary block !
        //
        //var syntaxElements2 = new List<SyntaxElement>();
        //var allNodes = syntaxTree.GetCompilationUnitRoot().DescendantNodesAndTokensAndSelf(descendIntoTrivia: true);
        //foreach(var node in allNodes)
        //{
        //    syntaxElements2.Add(new SyntaxElement(
        //        span: node.Span,
        //        kind: node.Kind()));
        //}

        //return syntaxElements2.ToImmutableArray();

        var compilationUnitRoot = syntaxTree.GetCompilationUnitRoot();
        var syntaxElements = new List<SyntaxElement>();
        RecursiveAddNode(compilationUnitRoot, syntaxElements);
        RemoveDuplicates(syntaxElements);
        return syntaxElements.ToImmutableArray();
    }

    private static void RemoveDuplicates(List<SyntaxElement> syntaxElements)
    {
        var bySpanStart = syntaxElements.OrderBy(s => s.Span.Start);
        var duplicateFree = new List<SyntaxElement>();

        foreach(var element in bySpanStart)
        {
            if(duplicateFree.Count == 0)
            {
                duplicateFree.Add(element);
            }
            else
            {
                var lastNonDuplicatedElement = duplicateFree.Last();

                if(element != lastNonDuplicatedElement)
                {
                    duplicateFree.Add(element);
                }
            }
        }

        syntaxElements.Clear();
        syntaxElements.AddRange(duplicateFree);
    }


    private static void RecursiveAddNode(SyntaxNode syntaxNode, List<SyntaxElement> syntaxElements)
    {
        syntaxElements.Add(new SyntaxElement(
            span: syntaxNode.Span,
            kind: syntaxNode.Kind()));

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

        foreach (SyntaxTrivia trailingTrivia in syntaxNode.GetTrailingTrivia())
        {
            RecursiveAddTrivia(syntaxElements, trailingTrivia);
        }
    }

    private static void RecursiveAddTrivia(List<SyntaxElement> syntaxElements, SyntaxTrivia leadingTrivia)
    {
        var structure = leadingTrivia.GetStructure();
        if (structure != null)
        {
            RecursiveAddNode(structure, syntaxElements);
        }

        syntaxElements.Add(new SyntaxElement(
            span: leadingTrivia.Span,
            kind: leadingTrivia.Kind()));
    }

    private static void AddToken(SyntaxToken syntaxToken, List<SyntaxElement> syntaxElements)
    {
        syntaxElements.Add(new SyntaxElement(
            span: syntaxToken.Span,
            kind: syntaxToken.Kind()));
    }
}
