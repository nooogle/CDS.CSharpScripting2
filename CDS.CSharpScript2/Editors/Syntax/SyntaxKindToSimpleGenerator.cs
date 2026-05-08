//using Microsoft.CodeAnalysis.CSharp;
//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;

//namespace CDS.CSharpScript2.Editors.Syntax;

//public static class SyntaxKindToSimpleGenerator
//{
//    public static ImmutableDictionary<SyntaxKind, ClassificationKind> Map { get; }

//    static SyntaxKindToSimpleGenerator()
//    {
//        var mutableMap = new Dictionary<SyntaxKind, ClassificationKind>();

//        foreach (var keywordKind in Enum
//            .GetNames(typeof(SyntaxKind))
//            .Where(n => n.ToLower().Contains("keyword"))
//            .Select(n => (SyntaxKind)Enum.Parse(typeof(SyntaxKind), n)))
//        {
//            mutableMap[keywordKind] = ClassificationKind.Keyword;
//        }

//        mutableMap[SyntaxKind.PredefinedType] = ClassificationKind.Keyword;

//        mutableMap[SyntaxKind.Argument] = ClassificationKind.Argument;

//        mutableMap[SyntaxKind.SingleLineCommentTrivia] = ClassificationKind.Commment;

//        mutableMap[SyntaxKind.DocumentationCommentExteriorTrivia] = ClassificationKind.XMLDocumentationComment;
//        mutableMap[SyntaxKind.SingleLineDocumentationCommentTrivia] = ClassificationKind.XMLDocumentationComment;
//        mutableMap[SyntaxKind.MultiLineDocumentationCommentTrivia] = ClassificationKind.XMLDocumentationComment;

//        Map = mutableMap.ToImmutableDictionary();
//    }
//}
