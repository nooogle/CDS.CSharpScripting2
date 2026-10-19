using Avalonia.Input;
using Avalonia.Interactivity;
using CDS.CSharpScript2.Editors;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <content>
/// Hooks for a documentation panel: the F1 help request, caret movement, and a manager document
/// that is guaranteed to match the text on screen.
/// </content>
public partial class AvaloniaScriptEditor
{
    /// <summary>
    /// Occurs when the user asks for help on the code at the caret (F1) or <see cref="RequestHelp"/> is called.
    /// </summary>
    public event EventHandler<HelpRequestedEventArgs>? HelpRequested;

    /// <summary>
    /// Occurs when the caret moves.
    /// </summary>
    public event EventHandler? CaretMoved;

    /// <summary>
    /// Gets the caret's character offset in the script.
    /// </summary>
    public int CaretOffset => _textEditor.CaretOffset;

    /// <summary>
    /// Raises <see cref="HelpRequested"/> at the caret.
    /// </summary>
    public void RequestHelp()
    {
        HelpRequested?.Invoke(this, new HelpRequestedEventArgs(CaretOffset));
    }

    /// <summary>
    /// Brings the manager's document level with the text on screen, so a request answered by the
    /// manager sees edits the debounced analysis passes have not caught up with yet.
    /// </summary>
    /// <param name="cancellationToken">A token that abandons the refresh.</param>
    /// <returns>A task that completes when the manager's document is current.</returns>
    internal async Task EnsureManagerDocumentCurrentAsync(CancellationToken cancellationToken)
    {
        var manager = _manager;
        if (manager is null || _disposed)
        {
            return;
        }

        var documentVersion = _documentVersion;
        if (_managerDocumentVersion != documentVersion)
        {
            _managerDocumentVersion = documentVersion;

            // No ConfigureAwait(false): the manager is single-threaded and the next call must stay
            // on the UI thread.
            await manager.UpdateScriptDocumentAsync(Script, cancellationToken);
        }
    }

    private void InitialiseHelp()
    {
        var textArea = _textEditor.TextArea;
        textArea.AddHandler(KeyDownEvent, OnTextAreaKeyDownForHelp, RoutingStrategies.Tunnel);
        textArea.Caret.PositionChanged += OnCaretPositionChanged;
    }

    private void DetachHelp()
    {
        var textArea = _textEditor.TextArea;
        textArea.RemoveHandler(KeyDownEvent, OnTextAreaKeyDownForHelp);
        textArea.Caret.PositionChanged -= OnCaretPositionChanged;
    }

    private void OnTextAreaKeyDownForHelp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1 && e.KeyModifiers == KeyModifiers.None)
        {
            RequestHelp();
            e.Handled = true;
        }
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        CaretMoved?.Invoke(this, EventArgs.Empty);
    }
}
