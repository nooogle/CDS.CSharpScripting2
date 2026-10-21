using CDS.CSharpScript2.Classification;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// The docs panel's colours and code font, derived from an <see cref="EditorTheme"/> so the panel
/// matches the editor: its background and foreground, a faint foreground border, and accent colours
/// (links, code, the state chip) that have a light and a dark pair chosen by the background's brightness.
/// </summary>
internal sealed class DocsPanelPalette
{
    public DocsPanelPalette(EditorTheme theme, string codeFontName)
    {
        var foreground = theme.Foreground;
        var background = theme.Background;
        var isDark = background.GetBrightness() < 0.5f;

        CodeFontName = codeFontName;
        Background = background;
        Foreground = foreground;
        Border = Blend(foreground, background, 96);
        Dim = Blend(foreground, background, 150);
        Heading = Blend(foreground, background, 175);
        Inset = Blend(foreground, background, 18);
        ButtonBackground = Blend(foreground, background, 28);
        CurrentBackground = theme.SelectionBackground;
        CurrentForeground = theme.SelectionForeground;

        Link = isDark ? Color.FromArgb(79, 193, 255) : Color.FromArgb(0, 102, 204);
        Code = isDark ? Color.FromArgb(156, 220, 254) : Color.FromArgb(0, 16, 128);
        FollowingChipBackground = isDark ? Color.FromArgb(14, 58, 92) : Color.FromArgb(219, 236, 255);
        FollowingChipForeground = isDark ? Color.FromArgb(156, 211, 255) : Color.FromArgb(11, 83, 148);
        BrowsingChipBackground = isDark ? Color.FromArgb(74, 58, 18) : Color.FromArgb(255, 240, 204);
        BrowsingChipForeground = isDark ? Color.FromArgb(240, 198, 116) : Color.FromArgb(122, 90, 0);
    }

    public string CodeFontName { get; }

    public Color Background { get; }

    public Color Foreground { get; }

    public Color Border { get; }

    public Color Dim { get; }

    public Color Heading { get; }

    /// <summary>The faint box behind the signature.</summary>
    public Color Inset { get; }

    public Color ButtonBackground { get; }

    /// <summary>The highlight behind the overload being shown.</summary>
    public Color CurrentBackground { get; }

    public Color CurrentForeground { get; }

    public Color Link { get; }

    public Color Code { get; }

    public Color FollowingChipBackground { get; }

    public Color FollowingChipForeground { get; }

    public Color BrowsingChipBackground { get; }

    public Color BrowsingChipForeground { get; }

    /// <summary>
    /// Composites <paramref name="foreground"/> at <paramref name="alpha"/> over <paramref name="background"/>.
    /// WinForms has no alpha blending for text and control colours, so the Avalonia panel's translucent
    /// brushes become the opaque colour they would produce.
    /// </summary>
    private static Color Blend(Color foreground, Color background, int alpha)
    {
        int Mix(int f, int b) => (f * alpha + b * (255 - alpha)) / 255;
        return Color.FromArgb(Mix(foreground.R, background.R), Mix(foreground.G, background.G), Mix(foreground.B, background.B));
    }
}
