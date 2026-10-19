using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit.CodeCompletion;
using Microsoft.CodeAnalysis.Completion;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// The description shown beside the completion list for one entry: Roslyn's signature and XML-doc
/// summary as plain wrapped text.
/// </summary>
/// <remarks>
/// AvaloniaEdit reads <see cref="ICompletionData.Description"/> synchronously when the highlight
/// moves and opens its popup straight away, but Roslyn's lookup is asynchronous. So this view is
/// handed over empty and fills itself in when the lookup lands; until then — or for good, if there
/// is no description — it hides the <see cref="CompletionTipContentControl"/> hosting it, which
/// would otherwise show as an empty box.
/// </remarks>
internal sealed class CompletionDescriptionView : TextBlock
{
    private const double MaxDescriptionWidth = 450;

    public CompletionDescriptionView(Task<CompletionDescription?> description)
    {
        TextWrapping = TextWrapping.Wrap;
        MaxWidth = MaxDescriptionWidth;
        _ = ShowWhenLoadedAsync(description);
    }

    protected override Type StyleKeyOverride => typeof(TextBlock);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The popup's host is shared by every entry, so each view sets it for itself on arrival.
        UpdateHostVisibility();
    }

    /// <param name="description">
    /// The lookup, which reports a cancelled or failed request as <see langword="null"/>.
    /// </param>
    private async Task ShowWhenLoadedAsync(Task<CompletionDescription?> description)
    {
        Text = (await description)?.Text;
        UpdateHostVisibility();
    }

    private void UpdateHostVisibility()
    {
        if (this.FindAncestorOfType<CompletionTipContentControl>() is { } host)
        {
            host.IsVisible = !string.IsNullOrEmpty(Text);
        }
    }
}
