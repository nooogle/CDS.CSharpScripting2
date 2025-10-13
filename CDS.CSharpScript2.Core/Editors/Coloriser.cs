using Microsoft.CodeAnalysis.Classification;
using System.Drawing;

namespace CDS.CSharpScript2.Editors;

public class ClassificationColorScheme
{
    public Color Foreground { get; }
    public Color Background { get; }
    public bool Bold { get; }
    public bool Italics { get; }

    public ClassificationColorScheme(Color foreground, Color background, bool bold, bool italics)
    {
        Foreground = foreground;
        Background = background;
        Bold = bold;
        Italics = italics;
    }
}   

public class Coloriser
{
    private Dictionary<string, ClassificationColorScheme> _exactSchemes;
    private Dictionary<string, ClassificationColorScheme> _prefixSchemes;
    private ClassificationColorScheme _defaultScheme = new ClassificationColorScheme(Color.Black, Color.Transparent, false, false);

    public Coloriser()
    {
        _exactSchemes = new Dictionary<string, ClassificationColorScheme>()
        {
            // Classes and Types
            [ClassificationTypeNames.ClassName] = new ClassificationColorScheme(
                foreground: Color.FromArgb(43, 145, 175),  // Teal
                background: Color.Transparent, 
                bold: false, 
                italics: false),

            [ClassificationTypeNames.StructName] = new ClassificationColorScheme(
                foreground: Color.FromArgb(134, 198, 145),  // Light Green
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.InterfaceName] = new ClassificationColorScheme(
                foreground: Color.FromArgb(184, 215, 163),  // Pale Green
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.EnumName] = new ClassificationColorScheme(
                foreground: Color.FromArgb(184, 215, 163),  // Pale Green
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.DelegateName] = new ClassificationColorScheme(
                foreground: Color.FromArgb(43, 145, 175),  // Teal
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.RecordClassName] = new ClassificationColorScheme(
                foreground: Color.FromArgb(43, 145, 175),  // Teal
                background: Color.Transparent,
                bold: false,
                italics: false),

            // Members
            [ClassificationTypeNames.MethodName] = new ClassificationColorScheme(
                foreground: Color.FromArgb(111, 66, 193),  // Purple
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.PropertyName] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.FieldName] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.EventName] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.LocalName] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.ParameterName] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            // Keywords and Control Flow
            [ClassificationTypeNames.Keyword] = new ClassificationColorScheme(
                foreground: Color.FromArgb(0, 0, 255),  // Blue
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.ControlKeyword] = new ClassificationColorScheme(
                foreground: Color.FromArgb(175, 0, 219),  // Magenta
                background: Color.Transparent,
                bold: false,
                italics: false),

            // Comments
            [ClassificationTypeNames.Comment] = new ClassificationColorScheme(
                foreground: Color.FromArgb(0, 128, 0),  // Green
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.XmlDocCommentText] = new ClassificationColorScheme(
                foreground: Color.FromArgb(0, 128, 0),  // Green
                background: Color.Transparent,
                bold: false,
                italics: false),

            // Strings and Literals
            [ClassificationTypeNames.StringLiteral] = new ClassificationColorScheme(
                foreground: Color.FromArgb(163, 21, 21),  // Dark Red
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.VerbatimStringLiteral] = new ClassificationColorScheme(
                foreground: Color.FromArgb(163, 21, 21),  // Dark Red
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.NumericLiteral] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            // Operators and Punctuation
            [ClassificationTypeNames.Operator] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.OperatorOverloaded] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: true,
                italics: false),

            [ClassificationTypeNames.Punctuation] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            // Preprocessor
            [ClassificationTypeNames.PreprocessorKeyword] = new ClassificationColorScheme(
                foreground: Color.FromArgb(128, 128, 128),  // Gray
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.PreprocessorText] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            // Namespaces
            [ClassificationTypeNames.NamespaceName] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            // Special
            [ClassificationTypeNames.Identifier] = new ClassificationColorScheme(
                foreground: Color.Black,
                background: Color.Transparent,
                bold: false,
                italics: false),

            [ClassificationTypeNames.ExtensionMethodName] = new ClassificationColorScheme(
                foreground: Color.FromArgb(111, 66, 193),  // Purple
                background: Color.Transparent,
                bold: false,
                italics: false),
        };

        _prefixSchemes = new Dictionary<string, ClassificationColorScheme>()
        {
            ["xml doc comment"] = new ClassificationColorScheme(
                foreground: Color.FromArgb(96, 139, 78),  // Muted Green
                background: Color.Transparent, 
                bold: false, 
                italics: false),
        };
    }

    public string[] ClassificationNames => _exactSchemes.Keys
        .Concat(_prefixSchemes.Keys)
        .Distinct()
        .ToArray();

    public ClassificationColorScheme FromClassificationName(string classification)
    {
        if (_exactSchemes.TryGetValue(classification, out var exactScheme))
        {
            return exactScheme;
        }

        foreach (var prefix in _prefixSchemes.Keys)
        {
            if (classification.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return _prefixSchemes[prefix];
            }
        }

        return _defaultScheme;
    }
}
