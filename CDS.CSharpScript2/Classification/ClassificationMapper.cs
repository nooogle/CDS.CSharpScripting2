using Microsoft.CodeAnalysis.Classification;
using System.Collections.Immutable;

namespace CDS.CSharpScript2.Classification;


/// <summary>
/// Provides a mapping from Roslyn classification type names to our internal SymbolClassification enum.
/// </summary>
public class ClassificationMapper
{
    /// <summary>
    /// Immutable dictionary mapping Roslyn classification type names (e.g., "keyword", "identifier") to our SymbolClassification enum values.
    /// </summary>
    public ImmutableDictionary<string, SymbolClassification> Map { get; }


    /// <summary>
    /// Initializes the classification mapping with all relevant Roslyn classification type names mapped to our SymbolClassification enum values.
    /// </summary>
    public ClassificationMapper()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, SymbolClassification>();
        
        builder.Add(ClassificationTypeNames.Comment, SymbolClassification.Comment);
        builder.Add(ClassificationTypeNames.ExcludedCode, SymbolClassification.ExcludedCode);
        builder.Add(ClassificationTypeNames.Identifier, SymbolClassification.Identifier);
        builder.Add(ClassificationTypeNames.Keyword, SymbolClassification.Keyword);
        builder.Add(ClassificationTypeNames.ControlKeyword, SymbolClassification.ControlKeyword);
        builder.Add(ClassificationTypeNames.NumericLiteral, SymbolClassification.NumericLiteral);
        builder.Add(ClassificationTypeNames.Operator, SymbolClassification.Operator);
        builder.Add(ClassificationTypeNames.OperatorOverloaded, SymbolClassification.OperatorOverloaded);
        builder.Add(ClassificationTypeNames.PreprocessorKeyword, SymbolClassification.PreprocessorKeyword);
        builder.Add(ClassificationTypeNames.StringLiteral, SymbolClassification.StringLiteral);
        builder.Add(ClassificationTypeNames.WhiteSpace, SymbolClassification.WhiteSpace);
        builder.Add(ClassificationTypeNames.Text, SymbolClassification.Text);
        builder.Add(ClassificationTypeNames.PreprocessorText, SymbolClassification.PreprocessorText);
        builder.Add(ClassificationTypeNames.Punctuation, SymbolClassification.Punctuation);
        builder.Add(ClassificationTypeNames.VerbatimStringLiteral, SymbolClassification.VerbatimStringLiteral);
        builder.Add(ClassificationTypeNames.StringEscapeCharacter, SymbolClassification.StringEscapeCharacter);
        builder.Add(ClassificationTypeNames.ClassName, SymbolClassification.ClassName);
        builder.Add(ClassificationTypeNames.RecordClassName, SymbolClassification.RecordClassName);
        builder.Add(ClassificationTypeNames.DelegateName, SymbolClassification.DelegateName);
        builder.Add(ClassificationTypeNames.EnumName, SymbolClassification.EnumName);
        builder.Add(ClassificationTypeNames.InterfaceName, SymbolClassification.InterfaceName);
        builder.Add(ClassificationTypeNames.ModuleName, SymbolClassification.ModuleName);
        builder.Add(ClassificationTypeNames.StructName, SymbolClassification.StructName);
        builder.Add(ClassificationTypeNames.RecordStructName, SymbolClassification.RecordStructName);
        builder.Add(ClassificationTypeNames.TypeParameterName, SymbolClassification.TypeParameterName);
        builder.Add(ClassificationTypeNames.FieldName, SymbolClassification.FieldName);
        builder.Add(ClassificationTypeNames.EnumMemberName, SymbolClassification.EnumMemberName);
        builder.Add(ClassificationTypeNames.ConstantName, SymbolClassification.ConstantName);
        builder.Add(ClassificationTypeNames.LocalName, SymbolClassification.LocalName);
        builder.Add(ClassificationTypeNames.ParameterName, SymbolClassification.ParameterName);
        builder.Add(ClassificationTypeNames.MethodName, SymbolClassification.MethodName);
        builder.Add(ClassificationTypeNames.ExtensionMethodName, SymbolClassification.ExtensionMethodName);
        builder.Add(ClassificationTypeNames.PropertyName, SymbolClassification.PropertyName);
        builder.Add(ClassificationTypeNames.EventName, SymbolClassification.EventName);
        builder.Add(ClassificationTypeNames.NamespaceName, SymbolClassification.NamespaceName);
        builder.Add(ClassificationTypeNames.LabelName, SymbolClassification.LabelName);
        builder.Add(ClassificationTypeNames.XmlDocCommentAttributeName, SymbolClassification.XmlDocCommentAttributeName);
        builder.Add(ClassificationTypeNames.XmlDocCommentAttributeQuotes, SymbolClassification.XmlDocCommentAttributeQuotes);
        builder.Add(ClassificationTypeNames.XmlDocCommentAttributeValue, SymbolClassification.XmlDocCommentAttributeValue);
        builder.Add(ClassificationTypeNames.XmlDocCommentCDataSection, SymbolClassification.XmlDocCommentCDataSection);
        builder.Add(ClassificationTypeNames.XmlDocCommentComment, SymbolClassification.XmlDocCommentComment);
        builder.Add(ClassificationTypeNames.XmlDocCommentDelimiter, SymbolClassification.XmlDocCommentDelimiter);
        builder.Add(ClassificationTypeNames.XmlDocCommentEntityReference, SymbolClassification.XmlDocCommentEntityReference);
        builder.Add(ClassificationTypeNames.XmlDocCommentName, SymbolClassification.XmlDocCommentName);
        builder.Add(ClassificationTypeNames.XmlDocCommentProcessingInstruction, SymbolClassification.XmlDocCommentProcessingInstruction);
        builder.Add(ClassificationTypeNames.XmlDocCommentText, SymbolClassification.XmlDocCommentText);
        builder.Add(ClassificationTypeNames.XmlLiteralAttributeName, SymbolClassification.XmlLiteralAttributeName);
        builder.Add(ClassificationTypeNames.XmlLiteralAttributeQuotes, SymbolClassification.XmlLiteralAttributeQuotes);
        builder.Add(ClassificationTypeNames.XmlLiteralAttributeValue, SymbolClassification.XmlLiteralAttributeValue);
        builder.Add(ClassificationTypeNames.XmlLiteralCDataSection, SymbolClassification.XmlLiteralCDataSection);
        builder.Add(ClassificationTypeNames.XmlLiteralComment, SymbolClassification.XmlLiteralComment);
        builder.Add(ClassificationTypeNames.XmlLiteralDelimiter, SymbolClassification.XmlLiteralDelimiter);
        builder.Add(ClassificationTypeNames.XmlLiteralEmbeddedExpression, SymbolClassification.XmlLiteralEmbeddedExpression);
        builder.Add(ClassificationTypeNames.XmlLiteralEntityReference, SymbolClassification.XmlLiteralEntityReference);
        builder.Add(ClassificationTypeNames.XmlLiteralName, SymbolClassification.XmlLiteralName);
        builder.Add(ClassificationTypeNames.XmlLiteralProcessingInstruction, SymbolClassification.XmlLiteralProcessingInstruction);
        builder.Add(ClassificationTypeNames.XmlLiteralText, SymbolClassification.XmlLiteralText);
        builder.Add(ClassificationTypeNames.RegexComment, SymbolClassification.RegexComment);
        builder.Add(ClassificationTypeNames.RegexCharacterClass, SymbolClassification.RegexCharacterClass);
        builder.Add(ClassificationTypeNames.RegexAnchor, SymbolClassification.RegexAnchor);
        builder.Add(ClassificationTypeNames.RegexQuantifier, SymbolClassification.RegexQuantifier);
        builder.Add(ClassificationTypeNames.RegexGrouping, SymbolClassification.RegexGrouping);
        builder.Add(ClassificationTypeNames.RegexAlternation, SymbolClassification.RegexAlternation);
        builder.Add(ClassificationTypeNames.RegexText, SymbolClassification.RegexText);
        builder.Add(ClassificationTypeNames.RegexSelfEscapedCharacter, SymbolClassification.RegexSelfEscapedCharacter);
        builder.Add(ClassificationTypeNames.RegexOtherEscape, SymbolClassification.RegexOtherEscape);
        
        Map = builder.ToImmutable();
    }
}