using System.Drawing;

namespace CDS.CSharpScript2.Classification;

/// <summary>
/// Provides color schemes for syntax classification of code symbols.
/// </summary>
public class Coloriser
{
    private Dictionary<SymbolClassification, ClassificationColorScheme> _schemes;

    private ClassificationColorScheme CreateDefaultScheme() => new ClassificationColorScheme(foreground: Color.Black);


    public Coloriser()
    {
        _schemes = new Dictionary<SymbolClassification, ClassificationColorScheme>();

        // Initialize all symbols with default scheme
        foreach (SymbolClassification symbol in Enum.GetValues(typeof(SymbolClassification)))
        {
            _schemes[symbol] = CreateDefaultScheme();
        }

        // Classes and Types
        _schemes[SymbolClassification.ClassName] = new ClassificationColorScheme(Color.FromArgb(43, 145, 175)); // Teal
        _schemes[SymbolClassification.RecordClassName] = new ClassificationColorScheme(Color.FromArgb(43, 145, 175)); // Teal
        _schemes[SymbolClassification.DelegateName] = new ClassificationColorScheme(Color.FromArgb(43, 145, 175)); // Teal
        
        _schemes[SymbolClassification.StructName] = new ClassificationColorScheme(Color.FromArgb(134, 198, 145)); // Light Green
        _schemes[SymbolClassification.RecordStructName] = new ClassificationColorScheme(Color.FromArgb(134, 198, 145)); // Light Green
        
        _schemes[SymbolClassification.InterfaceName] = new ClassificationColorScheme(Color.FromArgb(184, 215, 163)); // Pale Green
        _schemes[SymbolClassification.EnumName] = new ClassificationColorScheme(Color.FromArgb(184, 215, 163)); // Pale Green

        // Members
        _schemes[SymbolClassification.MethodName] = new ClassificationColorScheme(Color.FromArgb(111, 66, 193)); // Purple
        _schemes[SymbolClassification.ExtensionMethodName] = new ClassificationColorScheme(Color.FromArgb(111, 66, 193)); // Purple

        // Keywords and Control Flow
        _schemes[SymbolClassification.Keyword] = new ClassificationColorScheme(Color.FromArgb(0, 0, 255)); // Blue
        _schemes[SymbolClassification.ControlKeyword] = new ClassificationColorScheme(Color.FromArgb(175, 0, 219)); // Magenta

        // Comments
        _schemes[SymbolClassification.Comment] = new ClassificationColorScheme(Color.FromArgb(0, 128, 0)); // Green
        _schemes[SymbolClassification.XmlDocCommentText] = new ClassificationColorScheme(Color.FromArgb(0, 128, 0)); // Green
        
        // Other XML doc comment elements use a muted green
        _schemes[SymbolClassification.XmlDocCommentAttributeName] = new ClassificationColorScheme(Color.FromArgb(96, 139, 78)); // Muted Green
        _schemes[SymbolClassification.XmlDocCommentAttributeQuotes] = new ClassificationColorScheme(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentAttributeValue] = new ClassificationColorScheme(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentCDataSection] = new ClassificationColorScheme(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentComment] = new ClassificationColorScheme(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentDelimiter] = new ClassificationColorScheme(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentEntityReference] = new ClassificationColorScheme(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentName] = new ClassificationColorScheme(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentProcessingInstruction] = new ClassificationColorScheme(Color.FromArgb(96, 139, 78));

        // Strings and Literals
        _schemes[SymbolClassification.StringLiteral] = new ClassificationColorScheme(Color.FromArgb(163, 21, 21)); // Dark Red
        _schemes[SymbolClassification.VerbatimStringLiteral] = new ClassificationColorScheme(Color.FromArgb(163, 21, 21)); // Dark Red
        _schemes[SymbolClassification.StringEscapeCharacter] = new ClassificationColorScheme(Color.FromArgb(163, 21, 21)); // Dark Red

        // Operators and Punctuation
        _schemes[SymbolClassification.OperatorOverloaded] = CreateDefaultScheme().WithBold();

        // Preprocessor
        _schemes[SymbolClassification.PreprocessorKeyword] = new ClassificationColorScheme(Color.FromArgb(128, 128, 128)); // Gray
    }

    /// <summary>
    /// Gets the color scheme for the specified classification.
    /// </summary>
    /// <param name="classification">The symbol classification.</param>
    /// <returns>The color scheme for the classification, or the default scheme if not found.</returns>
    public ClassificationColorScheme FromClassificationName(SymbolClassification classification)
    {
        if (_schemes.TryGetValue(classification, out var exactScheme))
        {
            return exactScheme;
        }

        return CreateDefaultScheme();
    }
}
