using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// Converts the core library's <see cref="System.Drawing.Color"/> theme values to Avalonia types.
/// </summary>
internal static class ColorConversions
{
    public static Color ToAvalonia(this System.Drawing.Color color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static IImmutableSolidColorBrush ToBrush(this System.Drawing.Color color)
    {
        return new ImmutableSolidColorBrush(color.ToAvalonia());
    }
}
