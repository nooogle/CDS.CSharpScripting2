using System.Drawing;

namespace CDS.CSharpScript2.Classification;

/// <summary>
/// Provides color schemes for syntax classification of code symbols.
/// </summary>
public class Coloriser
{
    private readonly Dictionary<SymbolClassification, ClassificationColorScheme> _schemes;

    private static ClassificationColorScheme CreateDefaultScheme() => new(foreground: Color.Black);


    /// <summary>
    /// Initializes a new instance of <see cref="Coloriser"/> with the default color scheme assignments.
    /// </summary>
    public Coloriser()
    {
        _schemes = [];

        // Initialize all symbols with default scheme
        foreach (SymbolClassification symbol in Enum.GetValues(typeof(SymbolClassification)))
        {
            _schemes[symbol] = CreateDefaultScheme();
        }

        // Classes and Types
        _schemes[SymbolClassification.ClassName] = new(Color.FromArgb(43, 145, 175)); // Teal
        _schemes[SymbolClassification.RecordClassName] = new(Color.FromArgb(43, 145, 175)); // Teal
        _schemes[SymbolClassification.DelegateName] = new(Color.FromArgb(43, 145, 175)); // Teal
        
        _schemes[SymbolClassification.StructName] = new(Color.FromArgb(134, 198, 145)); // Light Green
        _schemes[SymbolClassification.RecordStructName] = new(Color.FromArgb(134, 198, 145)); // Light Green
        
        _schemes[SymbolClassification.InterfaceName] = new(Color.FromArgb(184, 215, 163)); // Pale Green
        _schemes[SymbolClassification.EnumName] = new(Color.FromArgb(184, 215, 163)); // Pale Green

        // Members
        _schemes[SymbolClassification.MethodName] = new(Color.FromArgb(111, 66, 193)); // Purple
        _schemes[SymbolClassification.ExtensionMethodName] = new(Color.FromArgb(111, 66, 193)); // Purple

        // Keywords and Control Flow
        _schemes[SymbolClassification.Keyword] = new(Color.FromArgb(0, 0, 255)); // Blue
        _schemes[SymbolClassification.ControlKeyword] = new(Color.FromArgb(175, 0, 219)); // Magenta

        // Comments
        _schemes[SymbolClassification.Comment] = new(Color.FromArgb(0, 128, 0)); // Green
        _schemes[SymbolClassification.XmlDocCommentText] = new(Color.FromArgb(0, 128, 0)); // Green
        
        // Other XML doc comment elements use a muted green
        _schemes[SymbolClassification.XmlDocCommentAttributeName] = new(Color.FromArgb(96, 139, 78)); // Muted Green
        _schemes[SymbolClassification.XmlDocCommentAttributeQuotes] = new(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentAttributeValue] = new(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentCDataSection] = new(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentComment] = new(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentDelimiter] = new(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentEntityReference] = new(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentName] = new(Color.FromArgb(96, 139, 78));
        _schemes[SymbolClassification.XmlDocCommentProcessingInstruction] = new(Color.FromArgb(96, 139, 78));

        // Strings and Literals
        _schemes[SymbolClassification.StringLiteral] = new(Color.FromArgb(163, 21, 21)); // Dark Red
        _schemes[SymbolClassification.VerbatimStringLiteral] = new(Color.FromArgb(163, 21, 21)); // Dark Red
        _schemes[SymbolClassification.StringEscapeCharacter] = new(Color.FromArgb(163, 21, 21)); // Dark Red

        // Operators and Punctuation
        _schemes[SymbolClassification.OperatorOverloaded] = CreateDefaultScheme().WithBold();

        // Preprocessor
        _schemes[SymbolClassification.PreprocessorKeyword] = new(Color.FromArgb(128, 128, 128)); // Gray
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

