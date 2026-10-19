using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Rendering;
using CDS.CSharpScript2.Avalonia.Sample;
using CDS.CSharpScript2.AvaloniaEditor;

namespace CDS.CSharpScript2.Avalonia.UITests;

/// <summary>
/// Shows the sample's <see cref="MainWindow"/> headlessly and drives it with simulated input.
/// Must be created and used on the headless UI thread (inside <see cref="HeadlessTestSession.RunAsync"/>).
/// </summary>
internal sealed class SampleWindowDriver : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    public SampleWindowDriver()
    {
        Window = new MainWindow();
        Window.Show();

        Editor = Window.FindControl<AvaloniaScriptEditor>("ScriptEditor")!;
        TextEditor = (TextEditor)Editor.Content!;
        Output = Window.FindControl<TextBox>("OutputBox")!;
        Status = Window.FindControl<TextBlock>("StatusText")!;
    }

    public MainWindow Window { get; }

    public AvaloniaScriptEditor Editor { get; }

    public TextEditor TextEditor { get; }

    public TextBox Output { get; }

    public TextBlock Status { get; }

    /// <summary>
    /// Focuses the editor, moves the caret to the end, and types <paramref name="text"/> one
    /// character at a time, as a user would (each character is its own text-input event).
    /// </summary>
    public void TypeAtEnd(string text)
    {
        TextEditor.TextArea.Focus();
        TextEditor.CaretOffset = TextEditor.Document.TextLength;
        Type(text);
    }

    /// <summary>Types <paramref name="text"/> at the caret, one character at a time.</summary>
    public void Type(string text)
    {
        foreach (var ch in text)
        {
            Window.KeyTextInput(ch.ToString());
        }
    }

    /// <summary>Replaces the script and puts the caret at its end, with the editor focused.</summary>
    public void SetScriptWithCaretAtEnd(string script)
    {
        Editor.Script = script;
        TextEditor.TextArea.Focus();
        TextEditor.CaretOffset = TextEditor.Document.TextLength;
    }

    /// <summary>Presses and releases a key with no modifiers.</summary>
    public void PressKey(Key key, PhysicalKey physicalKey)
    {
        PressKey(key, physicalKey, RawInputModifiers.None);
    }

    /// <summary>Presses and releases a key with the given modifiers held.</summary>
    public void PressKey(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers)
    {
        Window.KeyPress(key, modifiers, physicalKey, null);
        Window.KeyRelease(key, modifiers, physicalKey, null);
    }

    /// <summary>
    /// The entries the open completion list is actually displaying, or empty when it is closed.
    /// </summary>
    /// <remarks>
    /// Bounded by the list box's <c>ItemCount</c>, which is what it lays out and renders. Neither
    /// <c>CompletionData</c> nor enumerating <c>Items</c> will do: both read the underlying
    /// collection directly, so they still list entries the list box was never notified of — the
    /// exact failure where the popup holds data but renders empty.
    /// </remarks>
    public IReadOnlyList<string> CompletionItems
    {
        get
        {
            if (Editor.ActiveCompletionWindow?.CompletionList.ListBox is not { } listBox)
            {
                return [];
            }

            return Enumerable.Range(0, listBox.ItemCount)
                .Select(i => ((AvaloniaEdit.CodeCompletion.ICompletionData)listBox.Items[i]!).Text)
                .ToList();
        }
    }

    /// <summary>The highlighted entry in the open completion list, if any.</summary>
    public string? SelectedCompletion => Editor.ActiveCompletionWindow?.CompletionList.SelectedItem?.Text;

    /// <summary>
    /// The popup AvaloniaEdit shows beside the completion list for the highlighted entry's
    /// description, or <see langword="null"/> when no list is open.
    /// </summary>
    public Popup? CompletionDescriptionPopup =>
        Editor.ActiveCompletionWindow?.GetLogicalChildren().OfType<Popup>().FirstOrDefault();

    /// <summary>
    /// The text the description popup is showing, or <see langword="null"/> when it is closed or
    /// hidden while its description loads.
    /// </summary>
    public string? CompletionDescription
    {
        get
        {
            if (CompletionDescriptionPopup is not { IsOpen: true, Child: CompletionTipContentControl { IsVisible: true } tip })
            {
                return null;
            }

            return (tip.Content as TextBlock)?.Text;
        }
    }

    /// <summary>Waits until the description popup shows text containing <paramref name="text"/>.</summary>
    public Task WaitForCompletionDescriptionAsync(string text)
    {
        return WaitUntilAsync(
            () => CompletionDescription?.Contains(text, StringComparison.Ordinal) == true,
            $"completion description containing '{text}'");
    }

    /// <summary>Waits until the completion list is open with <paramref name="text"/> highlighted.</summary>
    public Task WaitForSelectedCompletionAsync(string text)
    {
        return WaitUntilAsync(() => SelectedCompletion == text, $"completion list with '{text}' highlighted");
    }

    /// <summary>Moves the pointer over the character at <paramref name="offset"/> in the script.</summary>
    public void HoverOverOffset(int offset)
    {
        var textView = TextEditor.TextArea.TextView;
        var location = TextEditor.Document.GetLocation(offset);
        var visual = textView.GetVisualPosition(new TextViewPosition(location), VisualYPosition.LineMiddle)
            - textView.ScrollOffset;

        // Aim a few pixels into the character rather than at its leading edge.
        var target = textView.TranslatePoint(visual + new Vector(3, 0), Window)
            ?? throw new InvalidOperationException("Text view is not in the window.");

        // Arrive from nearby so the text view sees a genuine move, then rest for the hover delay.
        Window.MouseMove(target + new Vector(-20, 0), RawInputModifiers.None);
        Window.MouseMove(target, RawInputModifiers.None);
    }

    /// <summary>
    /// Pumps the headless UI (layout, render, timers) until <paramref name="condition"/> holds,
    /// failing the test if it does not within the timeout.
    /// </summary>
    public async Task WaitUntilAsync(Func<bool> condition, string description)
    {
        var deadline = DateTime.UtcNow + DefaultTimeout;

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail($"Timed out after {DefaultTimeout.TotalSeconds:0}s waiting for: {description}");
            }

            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            await Task.Delay(20);
        }
    }

    /// <summary>Waits for the editor's initial analysis pass to report.</summary>
    public Task WaitForInitialAnalysisAsync()
    {
        return WaitUntilAsync(() => Status.Text != "Analysing…", "initial analysis to complete");
    }

    public void Dispose()
    {
        Window.Close();
    }
}
