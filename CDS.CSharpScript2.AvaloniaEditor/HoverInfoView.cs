using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CDS.CSharpScript2.APIInfo;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// The content of the hover popup: any diagnostics at the pointer, then compact information about
/// the symbol there — its signature in the editor's font (at the popup's own size, so it does
/// not dwarf the summary) and a one-paragraph summary.
/// </summary>
internal sealed class HoverInfoView : StackPanel
{
    private const double MaxPopupWidth = 500;

    public HoverInfoView(
        IReadOnlyList<string> diagnostics,
        QuickInfo? quickInfo,
        FontFamily codeFontFamily,
        IBrush dividerBrush)
    {
        MaxWidth = MaxPopupWidth;
        Spacing = 4;

        DiagnosticText = string.Join("\n", diagnostics);
        SignatureText = quickInfo?.Signature ?? string.Empty;
        SummaryText = quickInfo?.Summary ?? string.Empty;

        if (DiagnosticText.Length > 0)
        {
            Children.Add(new TextBlock { Text = DiagnosticText, TextWrapping = TextWrapping.Wrap });
        }

        if (DiagnosticText.Length > 0 && SignatureText.Length > 0)
        {
            Children.Add(new Border
            {
                Height = 1,
                Margin = new Thickness(0, 2),
                Background = dividerBrush,
            });
        }

        if (SignatureText.Length > 0)
        {
            Children.Add(new TextBlock
            {
                Text = SignatureText,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = codeFontFamily,
            });
        }

        if (SummaryText.Length > 0)
        {
            Children.Add(new TextBlock { Text = SummaryText, TextWrapping = TextWrapping.Wrap });
        }
    }

    /// <summary>The diagnostic messages shown, one per line; empty when there are none.</summary>
    public string DiagnosticText { get; }

    /// <summary>The symbol's signature; empty when no symbol is described.</summary>
    public string SignatureText { get; }

    /// <summary>The symbol's summary; empty when it has none.</summary>
    public string SummaryText { get; }

    /// <summary>True when there is nothing worth showing.</summary>
    public bool IsEmpty => DiagnosticText.Length == 0 && SignatureText.Length == 0;
}
