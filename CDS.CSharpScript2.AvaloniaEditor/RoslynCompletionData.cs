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
    private readonly Func<CompletionItem, Task<CompletionDescription?>> _describe;
    private CompletionDescriptionView? _description;

    /// <param name="item">The Roslyn item.</param>
    /// <param name="describe">
    /// Looks up the item's description; called at most once, the first time the entry is highlighted.
    /// </param>
    public RoslynCompletionData(CompletionItem item, Func<CompletionItem, Task<CompletionDescription?>> describe)
    {
        Item = item;
        _describe = describe;
    }

    public CompletionItem Item { get; }

    public string Text => Item.DisplayText;

    public object Content => Item.DisplayText;

    // Created on first highlight rather than up front: a list can hold thousands of entries and
    // only the few the user visits need a Roslyn lookup. Kept, so returning to an entry is instant.
    public object Description => _description ??= new CompletionDescriptionView(_describe(Item));

    public IImage? Image => null;

    // The list arrives already ranked by the core's CompletionMatcher; AvaloniaEdit's own
    // priority-based ordering is not used.
    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}
