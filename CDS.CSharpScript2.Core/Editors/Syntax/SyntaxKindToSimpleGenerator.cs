using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CDS.CSharpScript2.Editors.Syntax;

public static class SyntaxKindToSimpleGenerator
{
    public static ImmutableDictionary<SyntaxKind, SimpleSyntaxKind> Map { get; }

    static SyntaxKindToSimpleGenerator()
    {
        var mutableMap = new Dictionary<SyntaxKind, SimpleSyntaxKind>();

        foreach (var keywordKind in Enum
            .GetNames(typeof(SyntaxKind))
            .Where(n => n.ToLower().Contains("keyword"))
            .Select(n => (SyntaxKind)Enum.Parse(typeof(SyntaxKind), n)))
        {
            mutableMap[keywordKind] = SimpleSyntaxKind.Keyword;
        }

        mutableMap[SyntaxKind.PredefinedType] = SimpleSyntaxKind.Keyword;

        mutableMap[SyntaxKind.Argument] = SimpleSyntaxKind.Argument;

        mutableMap[SyntaxKind.SingleLineCommentTrivia] = SimpleSyntaxKind.Commment;

        mutableMap[SyntaxKind.DocumentationCommentExteriorTrivia] = SimpleSyntaxKind.XMLDocumentationComment;
        mutableMap[SyntaxKind.SingleLineDocumentationCommentTrivia] = SimpleSyntaxKind.XMLDocumentationComment;
        mutableMap[SyntaxKind.MultiLineDocumentationCommentTrivia] = SimpleSyntaxKind.XMLDocumentationComment;

        Map = mutableMap.ToImmutableDictionary();
    }
}
