using System.Drawing;

namespace CDS.CSharpScript2.Classification;

/// <summary>
/// A complete set of colors for an editor surface: the base background/foreground, caret line,
/// selection, brace matching, diagnostic indicators, fold margin, autocomplete list, and a color
/// scheme per <see cref="SymbolClassification"/>. Light and dark are just two instances of this
/// type rather than two separate code paths.
/// </summary>
/// <param name="Name">A short display name for the theme (e.g. "Light", "Dark").</param>
/// <param name="Background">The default editor background color.</param>
/// <param name="Foreground">The default text color, used for any classification without an explicit override.</param>
/// <param name="CaretLineBackground">The background color of the line containing the caret.</param>
/// <param name="SelectionBackground">The background color of selected text.</param>
/// <param name="SelectionForeground">The foreground color of selected text.</param>
/// <param name="BraceMatchForeground">The foreground color used to highlight a matched brace pair.</param>
/// <param name="BraceBadForeground">The foreground color used to highlight an unmatched brace.</param>
/// <param name="ErrorIndicatorForeColor">The color of the squiggle and gutter marker for error diagnostics.</param>
/// <param name="WarningIndicatorForeColor">The color of the squiggle and gutter marker for warning diagnostics.</param>
/// <param name="FoldMarginForeground">The foreground (glyph) color of the fold margin markers.</param>
/// <param name="FoldMarginBackground">The background (box) color of the fold margin markers.</param>
/// <param name="AutocompleteSelectedBackground">The background color of the highlighted entry in the autocomplete list.</param>
/// <param name="ClassificationColors">
/// Per-<see cref="SymbolClassification"/> color overrides. A classification not present here falls
/// back to <see cref="Foreground"/> with no background override, bold, italics, or underline; use
/// <see cref="GetClassificationColorScheme"/> rather than indexing this directly.
/// </param>
public sealed record EditorTheme(
    string Name,
    Color Background,
    Color Foreground,
    Color CaretLineBackground,
    Color SelectionBackground,
    Color SelectionForeground,
    Color BraceMatchForeground,
    Color BraceBadForeground,
    Color ErrorIndicatorForeColor,
    Color WarningIndicatorForeColor,
    Color FoldMarginForeground,
    Color FoldMarginBackground,
    Color AutocompleteSelectedBackground,
    IReadOnlyDictionary<SymbolClassification, ClassificationColorScheme> ClassificationColors)
{
    /// <summary>
    /// Gets the color scheme for the specified classification, falling back to <see cref="Foreground"/>
    /// with no other styling if the classification has no explicit override.
    /// </summary>
    /// <param name="classification">The symbol classification.</param>
    /// <returns>The color scheme for the classification.</returns>
    public ClassificationColorScheme GetClassificationColorScheme(SymbolClassification classification)
    {
        return ClassificationColors.TryGetValue(classification, out var scheme)
            ? scheme
            : new ClassificationColorScheme(Foreground);
    }

    /// <summary>
    /// The built-in light theme. Preserves the colors this editor used before theming existed.
    /// </summary>
    public static EditorTheme Light { get; } = new(
        Name: "Light",
        Background: Color.White,
        Foreground: Color.Black,
        CaretLineBackground: Color.FromArgb(255, 236, 240, 255),
        SelectionBackground: Color.FromArgb(173, 214, 255),
        SelectionForeground: Color.Black,
        BraceMatchForeground: Color.FromArgb(0, 120, 215),
        BraceBadForeground: Color.Red,
        ErrorIndicatorForeColor: Color.Red,
        WarningIndicatorForeColor: Color.DarkOrange,
        FoldMarginForeground: SystemColors.ControlLightLight,
        FoldMarginBackground: SystemColors.ControlDark,
        AutocompleteSelectedBackground: Color.FromArgb(0, 120, 212),
        ClassificationColors: CreateLightClassificationColors());

    /// <summary>
    /// The built-in dark theme.
    /// </summary>
    public static EditorTheme Dark { get; } = new(
        Name: "Dark",
        Background: Color.FromArgb(30, 30, 30),
        Foreground: Color.FromArgb(212, 212, 212),
        CaretLineBackground: Color.FromArgb(255, 45, 45, 48),
        SelectionBackground: Color.FromArgb(38, 79, 120),
        SelectionForeground: Color.FromArgb(212, 212, 212),
        BraceMatchForeground: Color.FromArgb(97, 175, 239),
        BraceBadForeground: Color.FromArgb(255, 92, 92),
        ErrorIndicatorForeColor: Color.FromArgb(244, 71, 71),
        WarningIndicatorForeColor: Color.FromArgb(255, 167, 38),
        FoldMarginForeground: Color.FromArgb(133, 133, 133),
        FoldMarginBackground: Color.FromArgb(60, 60, 60),
        AutocompleteSelectedBackground: Color.FromArgb(4, 93, 158),
        ClassificationColors: CreateDarkClassificationColors());

    private static Dictionary<SymbolClassification, ClassificationColorScheme> CreateLightClassificationColors()
    {
        return new Dictionary<SymbolClassification, ClassificationColorScheme>
        {
            // Classes and Types
            [SymbolClassification.ClassName] = new(Color.FromArgb(43, 145, 175)), // Teal
            [SymbolClassification.RecordClassName] = new(Color.FromArgb(43, 145, 175)), // Teal
            [SymbolClassification.DelegateName] = new(Color.FromArgb(43, 145, 175)), // Teal

            [SymbolClassification.StructName] = new(Color.FromArgb(134, 198, 145)), // Light Green
            [SymbolClassification.RecordStructName] = new(Color.FromArgb(134, 198, 145)), // Light Green

            [SymbolClassification.InterfaceName] = new(Color.FromArgb(184, 215, 163)), // Pale Green
            [SymbolClassification.EnumName] = new(Color.FromArgb(184, 215, 163)), // Pale Green

            // Members
            [SymbolClassification.MethodName] = new(Color.FromArgb(111, 66, 193)), // Purple
            [SymbolClassification.ExtensionMethodName] = new(Color.FromArgb(111, 66, 193)), // Purple

            // Keywords and Control Flow
            [SymbolClassification.Keyword] = new(Color.FromArgb(0, 0, 255)), // Blue
            [SymbolClassification.ControlKeyword] = new(Color.FromArgb(175, 0, 219)), // Magenta

            // Comments
            [SymbolClassification.Comment] = new(Color.FromArgb(0, 128, 0)), // Green
            [SymbolClassification.XmlDocCommentText] = new(Color.FromArgb(0, 128, 0)), // Green

            // Other XML doc comment elements use a muted green
            [SymbolClassification.XmlDocCommentAttributeName] = new(Color.FromArgb(96, 139, 78)),
            [SymbolClassification.XmlDocCommentAttributeQuotes] = new(Color.FromArgb(96, 139, 78)),
            [SymbolClassification.XmlDocCommentAttributeValue] = new(Color.FromArgb(96, 139, 78)),
            [SymbolClassification.XmlDocCommentCDataSection] = new(Color.FromArgb(96, 139, 78)),
            [SymbolClassification.XmlDocCommentComment] = new(Color.FromArgb(96, 139, 78)),
            [SymbolClassification.XmlDocCommentDelimiter] = new(Color.FromArgb(96, 139, 78)),
            [SymbolClassification.XmlDocCommentEntityReference] = new(Color.FromArgb(96, 139, 78)),
            [SymbolClassification.XmlDocCommentName] = new(Color.FromArgb(96, 139, 78)),
            [SymbolClassification.XmlDocCommentProcessingInstruction] = new(Color.FromArgb(96, 139, 78)),

            // Strings and Literals
            [SymbolClassification.StringLiteral] = new(Color.FromArgb(163, 21, 21)), // Dark Red
            [SymbolClassification.VerbatimStringLiteral] = new(Color.FromArgb(163, 21, 21)), // Dark Red
            [SymbolClassification.StringEscapeCharacter] = new(Color.FromArgb(163, 21, 21)), // Dark Red

            // Operators and Punctuation
            [SymbolClassification.OperatorOverloaded] = new ClassificationColorScheme(Color.Black).WithBold(),

            // Preprocessor
            [SymbolClassification.PreprocessorKeyword] = new(Color.FromArgb(128, 128, 128)), // Gray
        };
    }

    private static Dictionary<SymbolClassification, ClassificationColorScheme> CreateDarkClassificationColors()
    {
        return new Dictionary<SymbolClassification, ClassificationColorScheme>
        {
            // Classes and Types
            [SymbolClassification.ClassName] = new(Color.FromArgb(78, 201, 176)), // Teal
            [SymbolClassification.RecordClassName] = new(Color.FromArgb(78, 201, 176)), // Teal
            [SymbolClassification.DelegateName] = new(Color.FromArgb(78, 201, 176)), // Teal

            [SymbolClassification.StructName] = new(Color.FromArgb(134, 198, 145)), // Light Green
            [SymbolClassification.RecordStructName] = new(Color.FromArgb(134, 198, 145)), // Light Green

            [SymbolClassification.InterfaceName] = new(Color.FromArgb(184, 215, 163)), // Pale Green
            [SymbolClassification.EnumName] = new(Color.FromArgb(184, 215, 163)), // Pale Green

            // Members
            [SymbolClassification.MethodName] = new(Color.FromArgb(220, 220, 170)), // Soft Yellow
            [SymbolClassification.ExtensionMethodName] = new(Color.FromArgb(220, 220, 170)), // Soft Yellow

            // Keywords and Control Flow
            [SymbolClassification.Keyword] = new(Color.FromArgb(86, 156, 214)), // Blue
            [SymbolClassification.ControlKeyword] = new(Color.FromArgb(197, 134, 192)), // Magenta

            // Comments
            [SymbolClassification.Comment] = new(Color.FromArgb(106, 153, 85)), // Green
            [SymbolClassification.XmlDocCommentText] = new(Color.FromArgb(106, 153, 85)), // Green

            // Other XML doc comment elements use a muted green
            [SymbolClassification.XmlDocCommentAttributeName] = new(Color.FromArgb(122, 168, 102)),
            [SymbolClassification.XmlDocCommentAttributeQuotes] = new(Color.FromArgb(122, 168, 102)),
            [SymbolClassification.XmlDocCommentAttributeValue] = new(Color.FromArgb(122, 168, 102)),
            [SymbolClassification.XmlDocCommentCDataSection] = new(Color.FromArgb(122, 168, 102)),
            [SymbolClassification.XmlDocCommentComment] = new(Color.FromArgb(122, 168, 102)),
            [SymbolClassification.XmlDocCommentDelimiter] = new(Color.FromArgb(122, 168, 102)),
            [SymbolClassification.XmlDocCommentEntityReference] = new(Color.FromArgb(122, 168, 102)),
            [SymbolClassification.XmlDocCommentName] = new(Color.FromArgb(122, 168, 102)),
            [SymbolClassification.XmlDocCommentProcessingInstruction] = new(Color.FromArgb(122, 168, 102)),

            // Strings and Literals
            [SymbolClassification.StringLiteral] = new(Color.FromArgb(206, 145, 120)), // Salmon
            [SymbolClassification.VerbatimStringLiteral] = new(Color.FromArgb(206, 145, 120)), // Salmon
            [SymbolClassification.StringEscapeCharacter] = new(Color.FromArgb(206, 145, 120)), // Salmon

            // Operators and Punctuation
            [SymbolClassification.OperatorOverloaded] = new ClassificationColorScheme(Color.FromArgb(212, 212, 212)).WithBold(),

            // Preprocessor
            [SymbolClassification.PreprocessorKeyword] = new(Color.FromArgb(155, 155, 155)), // Gray
        };
    }
}
