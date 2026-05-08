using System.Drawing;

namespace CDS.CSharpScript2.Classification;

/// <summary>
/// Represents a color scheme for syntax classification, including foreground/background colors and text styling.
/// </summary>
public class ClassificationColorScheme
{
    /// <summary>
    /// Gets the foreground color for the classification.
    /// </summary>
    public Color Foreground { get; }

    /// <summary>
    /// Gets the background color for the classification.
    /// </summary>
    public Color Background { get; }

    /// <summary>
    /// Gets a value indicating whether the text should be rendered in bold.
    /// </summary>
    public bool Bold { get; }

    /// <summary>
    /// Gets a value indicating whether the text should be rendered in italics.
    /// </summary>
    public bool Italics { get; }

    /// <summary>
    /// Gets a value indicating whether the text should be underlined.
    /// </summary>
    public bool Underline { get; }

    private ClassificationColorScheme(
        Color foreground,
        Color background,
        bool bold,
        bool italics,
        bool underline)
    {
        Foreground = foreground;
        Background = background;
        Bold = bold;
        Italics = italics;
        Underline = underline;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassificationColorScheme"/> class with the specified foreground color.
    /// </summary>
    /// <param name="foreground">The foreground color.</param>
    public ClassificationColorScheme(Color foreground)
        : this(
            foreground: foreground,
            background: Color.Transparent,
            bold: false,
            italics: false,
            underline: false)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ClassificationColorScheme"/> with the specified foreground color.
    /// </summary>
    /// <param name="foreground">The new foreground color.</param>
    /// <returns>A new instance with the specified foreground color.</returns>
    public ClassificationColorScheme WithForeground(Color foreground)
    {
        return new ClassificationColorScheme(
            foreground: foreground,
            background: Background,
            bold: Bold,
            italics: Italics,
            underline: Underline);
    }

    /// <summary>
    /// Creates a new <see cref="ClassificationColorScheme"/> with the specified background color.
    /// </summary>
    /// <param name="background">The new background color.</param>
    /// <returns>A new instance with the specified background color.</returns>
    public ClassificationColorScheme WithBackground(Color background)
    {
        return new ClassificationColorScheme(
            foreground: Foreground,
            background: background,
            bold: Bold,
            italics: Italics,
            underline: Underline);
    }

    /// <summary>
    /// Creates a new <see cref="ClassificationColorScheme"/> with the specified bold setting.
    /// </summary>
    /// <param name="bold">Whether the text should be bold.</param>
    /// <returns>A new instance with the specified bold setting.</returns>
    public ClassificationColorScheme WithBold(bool bold = true)
    {
        return new ClassificationColorScheme(
            foreground: Foreground,
            background: Background,
            bold: bold,
            italics: Italics,
            underline: Underline);
    }

    /// <summary>
    /// Creates a new <see cref="ClassificationColorScheme"/> with the specified italics setting.
    /// </summary>
    /// <param name="italics">Whether the text should be italicized.</param>
    /// <returns>A new instance with the specified italics setting.</returns>
    public ClassificationColorScheme WithItalics(bool italics = true)
    {
        return new ClassificationColorScheme(
            foreground: Foreground,
            background: Background,
            bold: Bold,
            italics: italics,
            underline: Underline);
    }

    /// <summary>
    /// Creates a new <see cref="ClassificationColorScheme"/> with the specified underline setting.
    /// </summary>
    /// <param name="underline">Whether the text should be underlined.</param>
    /// <returns>A new instance with the specified underline setting.</returns>
    public ClassificationColorScheme WithUnderline(bool underline = true)
    {
        return new ClassificationColorScheme(
            foreground: Foreground,
            background: Background,
            bold: Bold,
            italics: Italics,
            underline: underline);
    }
}
