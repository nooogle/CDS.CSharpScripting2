using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CDS.CSharpScript2.Editors;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <content>
/// Hooks for a documentation panel: the F1 help request, caret movement, and a manager document
/// that is guaranteed to match the text on screen.
/// </content>
public partial class AvaloniaScriptEditor
{
    private Task? _managerRefreshTask;
    private int _managerRefreshVersion = -1;
    private EditorManager? _managerRefreshManager;

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
    /// The font the editor draws code in, so views beside it (the docs panel) can match it.
    /// </summary>
    internal FontFamily CodeFontFamily => _textEditor.FontFamily;

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
        if (_managerDocumentVersion == documentVersion)
        {
            return;
        }

        // Callers arriving during a refresh of the same version share it; each honours its own
        // token for its wait only, so one cancelled caller cannot strand the others.
        if (_managerRefreshTask is null
            || _managerRefreshVersion != documentVersion
            || !ReferenceEquals(_managerRefreshManager, manager))
        {
            var previous = _managerRefreshTask;
            _managerRefreshVersion = documentVersion;
            _managerRefreshManager = manager;
            _managerRefreshTask = RefreshManagerDocumentAsync(manager, documentVersion, Script, previous);
        }

        await _managerRefreshTask.WaitAsync(cancellationToken);
    }

    private async Task RefreshManagerDocumentAsync(
        EditorManager manager,
        int documentVersion,
        string script,
        Task? previous)
    {
        // The manager is single-threaded: never overlap with an earlier refresh. No
        // ConfigureAwait(false): the next manager call must stay on the UI thread.
        if (previous is not null)
        {
            try
            {
                await previous;
            }
            catch (Exception)
            {
                // That refresh's own callers see its failure; this one starts afresh.
            }
        }

        try
        {
            await manager.UpdateScriptDocumentAsync(script, CancellationToken.None);
        }
        catch (Exception)
        {
            // Leave the version unmarked and forget the failed task so the next caller retries.
            if (_managerRefreshVersion == documentVersion && ReferenceEquals(_managerRefreshManager, manager))
            {
                _managerRefreshTask = null;
            }

            throw;
        }

        // Mark the version only now the update has really happened.
        if (ReferenceEquals(_manager, manager))
        {
            _managerDocumentVersion = documentVersion;
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
