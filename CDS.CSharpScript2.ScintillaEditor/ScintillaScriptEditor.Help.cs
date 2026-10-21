using System.ComponentModel;
using CDS.CSharpScript2.Documentation;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <content>
/// Hooks for a documentation panel (<see cref="DocsPanel"/>): the F1 help request, caret movement,
/// theme changes, and documentation pages that are guaranteed to match the text on screen.
/// </content>
public partial class ScintillaScriptEditor
{
    /// <summary>
    /// Raised when the user asks for help on the code at the caret (F1) or <see cref="RequestHelp"/> is
    /// called. While nothing is subscribed, F1 shows the provisional API information window instead.
    /// </summary>
    [Category(CDSCategory)]
    public event EventHandler<Editors.HelpRequestedEventArgs>? ScriptHelpRequested;

    /// <summary>Raised when the caret moves, whether by the user or by an edit.</summary>
    [Category(CDSCategory)]
    public event EventHandler? CaretMoved;

    /// <summary>Raised after <see cref="Theme"/> has been changed and applied.</summary>
    [Category(CDSCategory)]
    public event EventHandler? ThemeChanged;

    /// <summary>Gets the caret's character offset in the script, or 0 once the editor is disposed.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CaretOffset => TryGetCurrentPosition(out var position) ? position : 0;

    /// <summary>Raises <see cref="ScriptHelpRequested"/> at the caret, as F1 does.</summary>
    public void RequestHelp()
    {
        ScriptHelpRequested?.Invoke(this, new Editors.HelpRequestedEventArgs(CaretOffset));
    }

    /// <summary>The font the editor draws code in, so views beside it can match it.</summary>
    internal string CodeFontName => scintilla.Styles[ScintillaNET.Style.Default].Font;

    /// <summary>Gives keyboard focus to the text area, so a view that closes can hand focus back.</summary>
    internal void FocusTextArea()
    {
        if (CanAccessEditor)
        {
            scintilla.Focus();
        }
    }

    /// <summary>
    /// Builds the documentation page for the symbol at <paramref name="position"/>, after bringing the
    /// manager's document level with the text on screen.
    /// </summary>
    /// <param name="position">The caret offset within the script.</param>
    /// <param name="cancellationToken">A token that abandons the request.</param>
    /// <returns>The page, or <see langword="null"/> when there is no symbol there or the editor has no environment.</returns>
    internal async Task<DocPage?> GetDocPageAsync(int position, CancellationToken cancellationToken)
    {
        var manager = _manager;
        var stateVersion = _editorStateVersion;
        var documentVersion = _documentVersion;

        // Awaits here deliberately keep the UI-thread context (no ConfigureAwait(false)): the checks after
        // them read editor state, and the caller is the UI.
        try
        {
            if (manager is null || !await EnsureManagerDocumentCurrentAsync(manager, stateVersion, cancellationToken))
            {
                return null;
            }

            // The position belongs to the text it was taken from; if the user has typed since, it no
            // longer points at the same symbol. The panel asks again when the caret next moves.
            if (documentVersion != _documentVersion)
            {
                return null;
            }

            return await manager.GetDocPageAsync(position, cancellationToken);
        }
        catch (ObjectDisposedException) when (IsStale(manager, stateVersion))
        {
            return null;
        }
    }

    /// <summary>
    /// Builds the documentation page for <paramref name="target"/>, after bringing the manager's
    /// document level with the text on screen.
    /// </summary>
    /// <param name="target">The link target to resolve.</param>
    /// <param name="cancellationToken">A token that abandons the request.</param>
    /// <returns>The page, or <see langword="null"/> when the editor has no environment.</returns>
    internal async Task<DocPage?> GetDocPageAsync(DocTarget target, CancellationToken cancellationToken)
    {
        var manager = _manager;
        var stateVersion = _editorStateVersion;

        try
        {
            if (manager is null || !await EnsureManagerDocumentCurrentAsync(manager, stateVersion, cancellationToken))
            {
                return null;
            }

            return await manager.GetDocPageAsync(target, cancellationToken);
        }
        catch (ObjectDisposedException) when (IsStale(manager, stateVersion))
        {
            return null;
        }
    }

    private void InitialiseHelp()
    {
        scintilla.UpdateUI += scintilla_UpdateUI_CaretMoved;
    }

    /// <summary>Raises <see cref="CaretMoved"/> when an update changed the selection (a caret move or an edit).</summary>
    private void scintilla_UpdateUI_CaretMoved(object? sender, ScintillaNET.UpdateUIEventArgs e)
    {
        if ((e.Change & ScintillaNET.UpdateChange.Selection) != 0)
        {
            CaretMoved?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Updates the manager's document to the text on screen unless the last analysis already did.
    /// The analysis pass is debounced, so without this a request right after typing would see old text.
    /// </summary>
    /// <returns><see langword="false"/> when the editor was invalidated while waiting, so the caller should give up.</returns>
    private async Task<bool> EnsureManagerDocumentCurrentAsync(
        Editors.EditorManager manager,
        int stateVersion,
        CancellationToken cancellationToken)
    {
        if (_analysedDocumentVersion != _documentVersion)
        {
            if (!TryGetScript(out var script))
            {
                return false;
            }

            await manager.UpdateScriptDocumentAsync(script, cancellationToken);
        }

        return !IsStale(manager, stateVersion);
    }

    private bool IsStale(Editors.EditorManager? manager, int stateVersion)
        => stateVersion != _editorStateVersion || !ReferenceEquals(manager, _manager) || !CanAccessEditor;
}
