using System.ComponentModel;
using CDS.CSharpScript2.Classification;
using CDS.CSharpScript2.Documentation;
using CDS.CSharpScript2.Editors;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// A documentation panel for a <see cref="ScintillaScriptEditor"/>: F1 in the editor shows the full
/// XML documentation for the symbol at the caret, and the panel then follows the caret.
/// </summary>
/// <remarks>
/// <para>
/// The panel is a plain control with no opinion about where it lives. It never shows or hides
/// itself: it raises <see cref="ShowRequested"/> when the user asks for help and
/// <see cref="CloseRequested"/> when they close it, and the host decides what that means — a fixed
/// column, a docking pane, a tab, or a floating form.
/// </para>
/// <para>
/// Connect it with <see cref="Editor"/>. It takes its colours from the editor's
/// <see cref="ScintillaScriptEditor.Theme"/> and follows changes to it.
/// </para>
/// </remarks>
public sealed class DocsPanel : UserControl
{
    internal const string FollowingText = "● Following caret";
    internal const string BrowsingText = "Browsing";
    internal const string FollowCaretText = "Follow caret (F1)";
    internal const string CloseText = "✕";

    private const int CaretFollowDelayMs = 300;

    private readonly Panel _header;
    private readonly Button _backButton;
    private readonly Button _forwardButton;
    private readonly Label _stateChip;
    private readonly Button _followButton;
    private readonly Button _closeButton;
    private readonly DocTextBox _body;
    private readonly Panel _bodyHost;
    private readonly System.Windows.Forms.Timer _caretTimer;
    private readonly ToolTip _toolTip = new();
    private readonly Font _smallFont = new("Segoe UI", 8f);
    private readonly Font _bodyFont = new("Segoe UI", 9f);

    private ScintillaScriptEditor? _editor;
    private EditorTheme _theme = EditorTheme.Light;
    private DocsPanelPalette _palette;

    // What the body was last drawn from, so unrelated changes (the Back button's state) don't redraw it.
    private DocPage? _renderedPage;
    private DocsPanelState _renderedState = DocsPanelState.Closed;
    private bool _renderedLoading;
    private EditorTheme? _renderedTheme;
    private string? _renderedCodeFont;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocsPanel"/> class.
    /// </summary>
    public DocsPanel()
    {
        Controller = new DocsPanelController(new CurrentEditorSource(this));
        Controller.Changed += (_, _) => Render();

        _palette = new DocsPanelPalette(_theme, CodeFontName);

        _caretTimer = new System.Windows.Forms.Timer { Interval = CaretFollowDelayMs };
        _caretTimer.Tick += OnCaretTimerTick;

        _backButton = HeaderButton("◀", "Back");
        _forwardButton = HeaderButton("▶", "Forward");
        _followButton = HeaderButton(FollowCaretText, "Show what is at the caret and follow it");
        _closeButton = HeaderButton(CloseText, "Close");
        _closeButton.Dock = DockStyle.Right;
        _backButton.Click += (_, _) => _ = Controller.BackAsync();
        _forwardButton.Click += (_, _) => _ = Controller.ForwardAsync();
        _followButton.Click += (_, _) => FollowCaret();
        _closeButton.Click += (_, _) => CloseFromUser();

        _stateChip = new Label
        {
            AutoSize = true,
            Padding = new Padding(6, 2, 6, 2),
            Margin = new Padding(6, 3, 0, 0),
            Font = _smallFont,
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = false,
        };
        buttons.Controls.Add(_backButton);
        buttons.Controls.Add(_forwardButton);
        buttons.Controls.Add(_stateChip);
        buttons.Controls.Add(_followButton);

        _header = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(4, 3, 4, 3) };
        _header.Controls.Add(buttons);
        _header.Controls.Add(_closeButton);
        _header.Paint += (_, e) => PaintBottomBorder(e.Graphics, _header);

        _body = new DocTextBox { Dock = DockStyle.Fill, Font = _bodyFont };
        _body.DocLinkClicked += OnDocLinkClicked;

        // The body is added first so docking gives the header the top strip and the body the rest.
        _bodyHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 6, 6) };
        _bodyHost.Controls.Add(_body);
        Controls.Add(_bodyHost);
        Controls.Add(_header);

        Padding = new Padding(1, 0, 0, 0);
        Render();
    }

    /// <summary>
    /// Occurs when the user asks for help in the connected editor (F1). The host should show the
    /// panel; the panel never shows itself.
    /// </summary>
    [Category("CDS")]
    public event EventHandler? ShowRequested;

    /// <summary>
    /// Occurs when the user closes the panel with its close button or Escape. The host should hide
    /// the panel; the panel never hides itself.
    /// </summary>
    [Category("CDS")]
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Gets or sets the editor the panel documents. Setting it starts listening for that editor's
    /// help requests, caret moves and theme changes, adopts its theme, and resets the panel to its
    /// empty state. Replacing the editor does not raise <see cref="CloseRequested"/>; the host
    /// decides whether to hide the panel.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ScintillaScriptEditor? Editor
    {
        get => _editor;
        set
        {
            if (ReferenceEquals(_editor, value))
            {
                return;
            }

            if (_editor is not null)
            {
                _editor.ScriptHelpRequested -= OnHelpRequested;
                _editor.CaretMoved -= OnCaretMoved;
                _editor.ThemeChanged -= OnEditorThemeChanged;
            }

            _caretTimer.Stop();
            _editor = value;

            if (_editor is not null)
            {
                _editor.ScriptHelpRequested += OnHelpRequested;
                _editor.CaretMoved += OnCaretMoved;
                _editor.ThemeChanged += OnEditorThemeChanged;
                _theme = _editor.Theme;
            }

            Controller.Close();
            Render();
        }
    }

    /// <summary>
    /// Gets or sets the colour theme. While an <see cref="Editor"/> is connected the panel follows the
    /// editor's theme, so set this only for a panel used without one.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EditorTheme Theme
    {
        get => _theme;
        set
        {
            _theme = value ?? throw new ArgumentNullException(nameof(value));
            Render();
        }
    }

    /// <summary>Gets the panel's mode: closed, following the caret, or browsing links.</summary>
    [Browsable(false)]
    public DocsPanelState State => Controller.State;

    /// <summary>
    /// Resets the panel (clears the page and history, and stops following the caret) without raising
    /// <see cref="CloseRequested"/>. Call this when you hide the panel by your own means, such as a
    /// docking pane's or form's own close button, so that reopening it with F1 starts fresh.
    /// </summary>
    public void Close()
    {
        var hadFocus = ContainsFocus;
        _caretTimer.Stop();
        Controller.Close();
        if (hadFocus)
        {
            _editor?.FocusTextArea();
        }
    }

    /// <summary>The panel's behaviour: following, browsing, and history.</summary>
    internal DocsPanelController Controller { get; }

    private string CodeFontName => _editor?.CodeFontName ?? "Consolas";

    /// <inheritdoc/>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Seen wherever focus is inside the panel, so a click on the body or a button still works.
        if (keyData == Keys.Escape)
        {
            CloseFromUser();
            return true;
        }

        if (keyData == Keys.F1)
        {
            // The editor handles F1 only while it has focus.
            FollowCaret();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(_palette.Border);
        e.Graphics.DrawLine(pen, 0, 0, 0, Height);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Editor = null;
            _caretTimer.Dispose();

            // Cancels any request still in flight; its result is then discarded.
            Controller.Close();
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
        if (disposing)
        {
            _smallFont.Dispose();
            _bodyFont.Dispose();
        }
    }

    private void FollowCaret()
    {
        _caretTimer.Stop();
        if (_editor is { } editor)
        {
            // The controller swallows cancellation and source failures, so fire-and-forget is safe.
            _ = Controller.ShowAtAsync(editor.CaretOffset);
        }
    }

    private void OnDocLinkClicked(object? sender, DocTarget target)
    {
        _caretTimer.Stop();
        _ = Controller.NavigateAsync(target);
    }

    private void OnHelpRequested(object? sender, HelpRequestedEventArgs e)
    {
        _caretTimer.Stop();

        // The controller swallows cancellation and source failures, so fire-and-forget is safe.
        _ = Controller.ShowAtAsync(e.Position);
        ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCaretMoved(object? sender, EventArgs e)
    {
        if (Controller.State != DocsPanelState.Following)
        {
            return;
        }

        // Follow after a pause, not on every keystroke or arrow press.
        _caretTimer.Stop();
        _caretTimer.Start();
    }

    private void OnCaretTimerTick(object? sender, EventArgs e)
    {
        _caretTimer.Stop();
        if (_editor is { } editor)
        {
            _ = Controller.CaretMovedAsync(editor.CaretOffset);
        }
    }

    // Raised by the editor after it applies a theme, so its Theme is current.
    private void OnEditorThemeChanged(object? sender, EventArgs e)
    {
        if (_editor is { } editor)
        {
            _theme = editor.Theme;
        }

        Render();
    }

    private void CloseFromUser()
    {
        var hadFocus = ContainsFocus;
        _caretTimer.Stop();
        Controller.Close();
        CloseRequested?.Invoke(this, EventArgs.Empty);

        // The panel is about to be hidden; don't leave keyboard focus on it.
        if (hadFocus)
        {
            _editor?.FocusTextArea();
        }
    }

    private void Render()
    {
        if (IsDisposed)
        {
            return;
        }

        var state = Controller.State;
        var browsing = state == DocsPanelState.Browsing;
        _palette = new DocsPanelPalette(_theme, CodeFontName);

        BackColor = _palette.Background;
        ForeColor = _palette.Foreground;
        _header.BackColor = _palette.Background;
        _bodyHost.BackColor = _palette.Background;
        _body.BackColor = _palette.Background;
        _body.ForeColor = _palette.Foreground;

        _backButton.Enabled = Controller.CanGoBack;
        _forwardButton.Enabled = Controller.CanGoForward;
        _followButton.Visible = browsing;

        foreach (var button in new[] { _backButton, _forwardButton, _followButton, _closeButton })
        {
            button.BackColor = _palette.ButtonBackground;
            button.ForeColor = button.Enabled ? _palette.Foreground : _palette.Dim;
        }

        _stateChip.Text = browsing ? BrowsingText : FollowingText;
        _stateChip.BackColor = browsing ? _palette.BrowsingChipBackground : _palette.FollowingChipBackground;
        _stateChip.ForeColor = browsing ? _palette.BrowsingChipForeground : _palette.FollowingChipForeground;

        var page = Controller.CurrentPage;
        var loading = Controller.IsLoading;
        if (ReferenceEquals(page, _renderedPage) &&
            state == _renderedState &&
            loading == _renderedLoading &&
            ReferenceEquals(_theme, _renderedTheme) &&
            _palette.CodeFontName == _renderedCodeFont)
        {
            Invalidate(true);
            return;
        }

        // A new page starts at the top; a redraw of the same page (theme, state) keeps its place.
        var scroll = ReferenceEquals(page, _renderedPage) ? _body.ScrollOffset : 0;

        _body.Links = DocPageRenderer.Render(_body, page, state, loading, _palette);
        _body.ScrollOffset = scroll;

        _renderedPage = page;
        _renderedState = state;
        _renderedLoading = loading;
        _renderedTheme = _theme;
        _renderedCodeFont = _palette.CodeFontName;
        Invalidate(true);
    }

    private Button HeaderButton(string text, string toolTip)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            Font = _smallFont,
            Margin = new Padding(0, 0, 4, 0),
            UseVisualStyleBackColor = false,
        };
        button.FlatAppearance.BorderSize = 0;
        _toolTip.SetToolTip(button, toolTip);
        return button;
    }

    private void PaintBottomBorder(Graphics graphics, Control control)
    {
        using var pen = new Pen(_palette.Border);
        graphics.DrawLine(pen, 0, control.Height - 1, control.Width, control.Height - 1);
    }

    /// <summary>Serves pages from whichever editor the panel is connected to at the time.</summary>
    private sealed class CurrentEditorSource : IDocPageSource
    {
        private readonly DocsPanel _panel;

        public CurrentEditorSource(DocsPanel panel)
        {
            _panel = panel;
        }

        public Task<DocPage?> GetDocPageAsync(int position, CancellationToken cancellationToken)
        {
            return _panel._editor?.GetDocPageAsync(position, cancellationToken)
                ?? Task.FromResult<DocPage?>(null);
        }

        public Task<DocPage?> GetDocPageAsync(DocTarget target, CancellationToken cancellationToken)
        {
            return _panel._editor?.GetDocPageAsync(target, cancellationToken)
                ?? Task.FromResult<DocPage?>(null);
        }
    }
}
