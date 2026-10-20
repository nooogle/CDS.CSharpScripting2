using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Editing;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// AvaloniaEdit's overload window, anchored under the call's opening parenthesis rather than the
/// caret, the way the Scintilla editor anchors its call tip.
/// </summary>
internal sealed class CallTipWindow : OverloadInsightWindow
{
    public CallTipWindow(TextArea textArea, int openParenPosition) : base(textArea)
    {
        // Starting just after the parenthesis means AvaloniaEdit closes the window when the caret
        // moves before it, or when a backspace deletes it.
        StartOffset = openParenPosition + 1;
        EndOffset = Math.Max(StartOffset, textArea.Caret.Offset);

        SetPosition(new TextViewPosition(textArea.Document.GetLocation(openParenPosition)));
    }
}
