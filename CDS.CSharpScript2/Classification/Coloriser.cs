namespace CDS.CSharpScript2.Classification;

/// <summary>
/// Looks up the color scheme for a syntax classification from an <see cref="EditorTheme"/>.
/// </summary>
public class Coloriser
{
    private readonly EditorTheme _theme;

    /// <summary>
    /// Initializes a new instance of <see cref="Coloriser"/> using <see cref="EditorTheme.Light"/>.
    /// </summary>
    public Coloriser()
        : this(EditorTheme.Light)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Coloriser"/> backed by the given theme.
    /// </summary>
    /// <param name="theme">The theme to look up classification color schemes from.</param>
    public Coloriser(EditorTheme theme)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    /// <summary>
    /// Gets the color scheme for the specified classification.
    /// </summary>
    /// <param name="classification">The symbol classification.</param>
    /// <returns>The color scheme for the classification, or the theme's default scheme if not found.</returns>
    public ClassificationColorScheme FromClassificationName(SymbolClassification classification)
    {
        return _theme.GetClassificationColorScheme(classification);
    }
}
