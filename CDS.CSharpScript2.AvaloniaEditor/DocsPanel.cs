using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using CDS.CSharpScript2.Classification;
using CDS.CSharpScript2.Documentation;
using CDS.CSharpScript2.Editors;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// A documentation panel for an <see cref="AvaloniaScriptEditor"/>: F1 in the editor shows the
/// full XML documentation for the symbol at the caret, and the panel then follows the caret.
/// </summary>
/// <remarks>
/// <para>
/// The panel is a plain control with no opinion about where it lives. It never shows or hides
/// itself: it raises <see cref="ShowRequested"/> when the user asks for help and
/// <see cref="CloseRequested"/> when they close it, and the host decides what that means — a
/// column, a docking pane, a tab, or a floating window.
/// </para>
/// <para>
/// Connect it with <see cref="Editor"/>. It takes its colours from the editor's
/// <see cref="AvaloniaScriptEditor.EffectiveTheme"/>, matching the editor's hover popup, and draws
/// code in the editor's font.
/// </para>
/// </remarks>
public sealed class DocsPanel : UserControl
{
    internal const string FollowingText = "● Following caret";
    internal const string BrowsingText = "Browsing";
    internal const string FollowCaretText = "Follow caret (F1)";
    internal const string CloseText = "✕";

    private static readonly TimeSpan CaretFollowDelay = TimeSpan.FromMilliseconds(300);
    private static readonly FontFamily s_fallbackCodeFont = new("Cascadia Code, Cascadia Mono, Menlo, Consolas, monospace");

    private readonly DispatcherTimer _caretTimer;
    private readonly Border _frame;
    private readonly Border _header;
    private readonly Button _backButton;
    private readonly Button _forwardButton;
    private readonly Border _stateChip;
    private readonly TextBlock _stateLabel;
    private readonly Button _followButton;
    private readonly Button _closeButton;
    private readonly ScrollViewer _scrollViewer;

    private AvaloniaScriptEditor? _editor;
    private EditorDocPageSource? _editorSource;
    private DocPage? _renderedPage;

    /// <summary>
    /// Initialises a new instance of the <see cref="DocsPanel"/> class.
    /// </summary>
    public DocsPanel()
    {
        Controller = new DocsPanelController(new CurrentEditorSource(this));
        Controller.Changed += (_, _) => Render();

        _caretTimer = new DispatcherTimer { Interval = CaretFollowDelay };
        _caretTimer.Tick += OnCaretTimerTick;

        _backButton = HeaderButton("◀", "Back");
        _forwardButton = HeaderButton("▶", "Forward");
        _followButton = HeaderButton(FollowCaretText, "Show what is at the caret and follow it");
        _closeButton = HeaderButton(CloseText, "Close");
        _closeButton.Click += (_, _) => CloseFromUser();

        _stateLabel = new TextBlock { FontSize = 10.5, VerticalAlignment = VerticalAlignment.Center };
        _stateChip = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = _stateLabel,
        };

        var bar = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,*,Auto") };
        Place(bar, _backButton, 0);
        Place(bar, _forwardButton, 1);
        Place(bar, _stateChip, 2);
        Place(bar, _followButton, 3);
        Place(bar, _closeButton, 5);
        _stateChip.Margin = new Thickness(6, 0, 0, 0);
        _followButton.Margin = new Thickness(6, 0, 0, 0);
        _forwardButton.Margin = new Thickness(4, 0, 0, 0);

        _header = new Border
        {
            Padding = new Thickness(8, 6),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = bar,
        };
        DockPanel.SetDock(_header, Dock.Top);

        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(12, 10),
        };

        var layout = new DockPanel { LastChildFill = true };
        layout.Children.Add(_header);
        layout.Children.Add(_scrollViewer);

        _frame = new Border { BorderThickness = new Thickness(1, 0, 0, 0), Child = layout };
        Content = _frame;

        // Focusable so Escape reaches the panel after a click anywhere in it.
        Focusable = true;
        AddHandler(PointerPressedEvent, OnPanelPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        ActualThemeVariantChanged += (_, _) => Render();

        Render();
    }

    /// <summary>
    /// Occurs when the user asks for help in the connected editor (F1). The host should show the
    /// panel; the panel never shows itself.
    /// </summary>
    public event EventHandler? ShowRequested;

    /// <summary>
    /// Occurs when the user closes the panel with its close button or Escape. The host should hide
    /// the panel; the panel never hides itself.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Gets or sets the editor the panel documents. Setting it starts listening for that editor's
    /// help requests and caret moves, and resets the panel.
    /// </summary>
    public AvaloniaScriptEditor? Editor
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
                _editor.HelpRequested -= OnHelpRequested;
                _editor.CaretMoved -= OnCaretMoved;
                _editor.ActualThemeVariantChanged -= OnEditorThemeChanged;
            }

            _caretTimer.Stop();
            _editor = value;
            _editorSource = value is null ? null : new EditorDocPageSource(value);

            if (_editor is not null)
            {
                _editor.HelpRequested += OnHelpRequested;
                _editor.CaretMoved += OnCaretMoved;
                _editor.ActualThemeVariantChanged += OnEditorThemeChanged;
            }

            Controller.Close();
            Render();
        }
    }

    /// <summary>The panel's behaviour: following, browsing, and history.</summary>
    internal DocsPanelController Controller { get; }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            CloseFromUser();
            e.Handled = true;
        }
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

    private void OnEditorThemeChanged(object? sender, EventArgs e) => Render();

    private void OnPanelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsKeyboardFocusWithin)
        {
            Focus();
        }
    }

    private void CloseFromUser()
    {
        _caretTimer.Stop();
        Controller.Close();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private DocsPanelPalette CreatePalette()
    {
        var theme = _editor?.EffectiveTheme
            ?? (ActualThemeVariant == ThemeVariant.Dark ? EditorTheme.Dark : EditorTheme.Light);
        return new DocsPanelPalette(theme, _editor?.CodeFontFamily ?? s_fallbackCodeFont);
    }

    private void Render()
    {
        var palette = CreatePalette();
        var state = Controller.State;
        var browsing = state == DocsPanelState.Browsing;

        _frame.Background = palette.Background;
        _frame.BorderBrush = palette.Border;
        _header.BorderBrush = palette.Border;
        Foreground = palette.Foreground;

        foreach (var button in new[] { _backButton, _forwardButton, _followButton, _closeButton })
        {
            button.Background = palette.ButtonBackground;
            button.Foreground = palette.Foreground;
        }

        _backButton.IsEnabled = Controller.CanGoBack;
        _forwardButton.IsEnabled = Controller.CanGoForward;
        _followButton.IsVisible = browsing;

        _stateLabel.Text = browsing ? BrowsingText : FollowingText;
        _stateChip.Background = browsing ? palette.BrowsingChipBackground : palette.FollowingChipBackground;
        _stateLabel.Foreground = browsing ? palette.BrowsingChipForeground : palette.FollowingChipForeground;

        var page = Controller.CurrentPage;
        _scrollViewer.Content = DocPageRenderer.Render(page, state, palette);

        // A new page starts at the top; a re-render of the same page (theme, state) keeps its place.
        if (!ReferenceEquals(page, _renderedPage))
        {
            _scrollViewer.Offset = default;
            _renderedPage = page;
        }
    }

    private static Button HeaderButton(string content, string toolTip)
    {
        var button = new Button
        {
            Content = content,
            FontSize = 11,
            Padding = new Thickness(6, 2),
            MinHeight = 0,
            CornerRadius = new CornerRadius(3),
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(button, toolTip);
        return button;
    }

    private static void Place(Grid grid, Control control, int column)
    {
        Grid.SetColumn(control, column);
        grid.Children.Add(control);
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
            return _panel._editorSource?.GetDocPageAsync(position, cancellationToken)
                ?? Task.FromResult<DocPage?>(null);
        }

        public Task<DocPage?> GetDocPageAsync(DocTarget target, CancellationToken cancellationToken)
        {
            return _panel._editorSource?.GetDocPageAsync(target, cancellationToken)
                ?? Task.FromResult<DocPage?>(null);
        }
    }
}
