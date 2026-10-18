using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Microsoft.CodeAnalysis.Completion;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// Adapts a Roslyn <see cref="CompletionItem"/> to an AvaloniaEdit completion list entry.
/// </summary>
internal sealed class RoslynCompletionData : ICompletionData
{
    public RoslynCompletionData(CompletionItem item)
    {
        Item = item;
    }

    public CompletionItem Item { get; }

    public string Text => Item.DisplayText;

    public object Content => Item.DisplayText;

    public object? Description => null;

    public IImage? Image => null;

    // The list arrives already ranked by the core's CompletionMatcher; AvaloniaEdit's own
    // priority-based ordering is not used.
    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}
