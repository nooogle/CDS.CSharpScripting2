using ScintillaNET;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// A non-modal Find / Find-Replace dialog for a <see cref="Scintilla"/> editor control.
/// </summary>
internal sealed partial class FormFindReplace : Form
{
    private readonly Scintilla _scintilla;

    // ── Construction ─────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="FormFindReplace"/> bound to the given Scintilla control.
    /// </summary>
    /// <param name="scintilla">The Scintilla editor to search within.</param>
    public FormFindReplace(Scintilla scintilla)
    {
        ArgumentNullException.ThrowIfNull(scintilla);
        _scintilla = scintilla;
        InitializeComponent();
        txtFindFind.KeyDown += txtFindFind_KeyDown;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the dialog in Find-only mode, pre-filling the search box with any currently selected text.
    /// </summary>
    public void OpenFind()
    {
        tabControl.SelectedTab = tabPageFind;
        PreFillSearchText();
        ShowOrActivate();
    }

    /// <summary>
    /// Shows the dialog in Find-and-Replace mode, pre-filling the search box with any currently selected text.
    /// </summary>
    public void OpenReplace()
    {
        tabControl.SelectedTab = tabPageReplace;
        PreFillSearchText();
        ShowOrActivate();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ShowOrActivate()
    {
        if (!Visible)
        {
            Show(_scintilla.FindForm());
        }

        Activate();
        txtFindFind.Focus();
        txtFindFind.SelectAll();
    }

    private void PreFillSearchText()
    {
        var selected = _scintilla.SelectedText;

        if (!string.IsNullOrEmpty(selected) && !selected.Contains('\n'))
        {
            txtFindFind.Text = selected;
            txtReplaceFind.Text = selected;
        }
    }

    /// <summary>
    /// Searches for the next or previous occurrence of <paramref name="searchText"/> starting from
    /// the current caret or selection, wrapping around if necessary.
    /// </summary>
    /// <param name="searchText">The text to find.</param>
    /// <param name="forward">Search direction.</param>
    /// <param name="matchCase">Whether the search is case-sensitive.</param>
    /// <param name="wholeWord">Whether to match whole words only.</param>
    /// <returns><see langword="true"/> if a match was found.</returns>
    private bool FindNext(string searchText, bool forward, bool matchCase, bool wholeWord)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            return false;
        }

        var flags = BuildSearchFlags(matchCase, wholeWord);
        _scintilla.SearchFlags = flags;

        int textLength = _scintilla.TextLength;
        int anchor = _scintilla.AnchorPosition;
        int caret = _scintilla.CurrentPosition;
        int start = forward ? Math.Max(anchor, caret) : Math.Min(anchor, caret);

        // First pass: from current position to the end (or start) of the document.
        int found = SearchInRange(searchText, forward, start, textLength);

        if (found < 0)
        {
            // Wrap around.
            found = SearchInRange(searchText, forward, forward ? 0 : textLength, textLength);
        }

        if (found >= 0)
        {
            _scintilla.SetSel(_scintilla.TargetStart, _scintilla.TargetEnd);
            _scintilla.ScrollCaret();
            return true;
        }

        MessageBox.Show(
            this,
            $"Cannot find \"{searchText}\".",
            "Find",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        return false;
    }

    private int SearchInRange(string searchText, bool forward, int start, int textLength)
    {
        if (forward)
        {
            _scintilla.SetTargetRange(start, textLength);
        }
        else
        {
            _scintilla.SetTargetRange(start, 0);
        }

        return _scintilla.SearchInTarget(searchText);
    }

    private static SearchFlags BuildSearchFlags(bool matchCase, bool wholeWord)
    {
        var flags = SearchFlags.None;

        if (matchCase) { flags |= SearchFlags.MatchCase; }
        if (wholeWord) { flags |= SearchFlags.WholeWord; }

        return flags;
    }

    // ── Find tab event handlers ───────────────────────────────────────────────

    private void btnFindNext_Click(object sender, EventArgs e)
    {
        FindNext(txtFindFind.Text, forward: true, chkFindMatchCase.Checked, chkFindWholeWord.Checked);
    }

    private void btnFindPrevious_Click(object sender, EventArgs e)
    {
        FindNext(txtFindFind.Text, forward: false, chkFindMatchCase.Checked, chkFindWholeWord.Checked);
    }

    private void txtFindFind_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            FindNext(txtFindFind.Text, forward: true, chkFindMatchCase.Checked, chkFindWholeWord.Checked);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    // ── Replace tab event handlers ────────────────────────────────────────────

    private void btnReplaceFindNext_Click(object sender, EventArgs e)
    {
        FindNext(txtReplaceFind.Text, forward: true, chkReplaceMatchCase.Checked, chkReplaceWholeWord.Checked);
    }

    private void btnReplaceReplace_Click(object sender, EventArgs e)
    {
        var searchText = txtReplaceFind.Text;

        if (string.IsNullOrEmpty(searchText))
        {
            return;
        }

        // If the current selection already matches the search text, replace it; otherwise find it first.
        var flags = BuildSearchFlags(chkReplaceMatchCase.Checked, chkReplaceWholeWord.Checked);
        _scintilla.SearchFlags = flags;
        _scintilla.SetTargetRange(_scintilla.SelectionStart, _scintilla.SelectionEnd);

        bool selectionMatches = _scintilla.SearchInTarget(searchText) >= 0;

        if (selectionMatches)
        {
            _scintilla.ReplaceTarget(txtReplaceWith.Text);
        }

        FindNext(searchText, forward: true, chkReplaceMatchCase.Checked, chkReplaceWholeWord.Checked);
    }

    private void btnReplaceAll_Click(object sender, EventArgs e)
    {
        var searchText = txtReplaceFind.Text;

        if (string.IsNullOrEmpty(searchText))
        {
            return;
        }

        var flags = BuildSearchFlags(chkReplaceMatchCase.Checked, chkReplaceWholeWord.Checked);
        _scintilla.SearchFlags = flags;
        _scintilla.SetTargetRange(0, _scintilla.TextLength);

        int count = 0;
        int found;

        _scintilla.BeginUndoAction();

        try
        {
            while ((found = _scintilla.SearchInTarget(searchText)) >= 0)
            {
                int replacementLength = _scintilla.ReplaceTarget(txtReplaceWith.Text);
                int nextStart = _scintilla.TargetStart + replacementLength;
                _scintilla.SetTargetRange(nextStart, _scintilla.TextLength);
                count++;
            }
        }
        finally
        {
            _scintilla.EndUndoAction();
        }

        MessageBox.Show(
            this,
            count == 0
                ? $"Cannot find \"{searchText}\"."
                : $"{count} replacement{(count == 1 ? string.Empty : "s")} made.",
            "Replace All",
            MessageBoxButtons.OK,
            count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Information);
    }

    // ── Form-level events ─────────────────────────────────────────────────────

    /// <summary>
    /// Hides the dialog instead of closing it so the instance can be reused.
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    /// <summary>
    /// Closes the dialog when the user presses Escape.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Hide();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
