using Avalonia.Media;
using CDS.CSharpScript2.Classification;
using DrawingColor = System.Drawing.Color;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// The docs panel's brushes and fonts, derived from the connected editor's theme so the panel
/// matches the hover popup: the editor's background and foreground, a faint foreground border, and
/// code in the editor's font. Accent colours (links, code, the state chip) have a light and a dark
/// pair, chosen by the theme background's brightness.
/// </summary>
internal sealed class DocsPanelPalette
{
    public DocsPanelPalette(EditorTheme theme, FontFamily codeFontFamily)
    {
        var foreground = theme.Foreground;
        var isDark = theme.Background.GetBrightness() < 0.5f;

        CodeFontFamily = codeFontFamily;
        Background = theme.Background.ToBrush();
        Foreground = foreground.ToBrush();
        Border = DrawingColor.FromArgb(96, foreground).ToBrush();
        Dim = DrawingColor.FromArgb(150, foreground).ToBrush();
        Heading = DrawingColor.FromArgb(175, foreground).ToBrush();
        Inset = DrawingColor.FromArgb(18, foreground).ToBrush();
        ButtonBackground = DrawingColor.FromArgb(28, foreground).ToBrush();
        CurrentBackground = theme.SelectionBackground.ToBrush();
        CurrentForeground = theme.SelectionForeground.ToBrush();

        Link = (isDark ? DrawingColor.FromArgb(79, 193, 255) : DrawingColor.FromArgb(0, 102, 204)).ToBrush();
        Code = (isDark ? DrawingColor.FromArgb(156, 220, 254) : DrawingColor.FromArgb(0, 16, 128)).ToBrush();
        FollowingChipBackground = (isDark ? DrawingColor.FromArgb(14, 58, 92) : DrawingColor.FromArgb(219, 236, 255)).ToBrush();
        FollowingChipForeground = (isDark ? DrawingColor.FromArgb(156, 211, 255) : DrawingColor.FromArgb(11, 83, 148)).ToBrush();
        BrowsingChipBackground = (isDark ? DrawingColor.FromArgb(74, 58, 18) : DrawingColor.FromArgb(255, 240, 204)).ToBrush();
        BrowsingChipForeground = (isDark ? DrawingColor.FromArgb(240, 198, 116) : DrawingColor.FromArgb(122, 90, 0)).ToBrush();
    }

    public FontFamily CodeFontFamily { get; }

    public IBrush Background { get; }

    public IBrush Foreground { get; }

    public IBrush Border { get; }

    public IBrush Dim { get; }

    public IBrush Heading { get; }

    /// <summary>The faint box behind the signature.</summary>
    public IBrush Inset { get; }

    public IBrush ButtonBackground { get; }

    /// <summary>The highlight behind the overload being shown.</summary>
    public IBrush CurrentBackground { get; }

    public IBrush CurrentForeground { get; }

    public IBrush Link { get; }

    public IBrush Code { get; }

    public IBrush FollowingChipBackground { get; }

    public IBrush FollowingChipForeground { get; }

    public IBrush BrowsingChipBackground { get; }

    public IBrush BrowsingChipForeground { get; }
}
