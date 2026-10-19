using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit;
using CDS.CSharpScript2.Classification;
using CDS.CSharpScript2.Editors;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// Cross-platform C# script editor control built on AvaloniaEdit, with Roslyn-driven syntax
/// colouring and live error/warning squiggles.
/// </summary>
/// <remarks>
/// Set <see cref="Environment"/> to start live analysis. Analysis runs in two cadences, matching
/// the Scintilla editor: a fast syntax-only pass colours newly typed code, and a slower debounced
/// full pass produces diagnostics and semantically refined colouring once typing pauses.
/// Dispose the control when it is no longer needed to release the underlying Roslyn workspace.
/// </remarks>
public partial class AvaloniaScriptEditor : UserControl, IScriptEditor
{
    private static readonly TimeSpan SyntacticPassDelay = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan FullPassDelay = TimeSpan.FromMilliseconds(500);

    private readonly TextEditor _textEditor;
    private readonly ClassificationColorizer _colorizer = new();
    private readonly DiagnosticRenderer _diagnosticRenderer;
    private readonly DispatcherTimer _syntacticTimer;
    private readonly DispatcherTimer _fullPassTimer;

    private ScriptEnvironment? _environment;
    private EditorManager? _manager;
    private EditorTheme? _theme;
    private ImmutableArray<Diagnostic> _currentDiagnostics = [];
    private ExecutableScript? _currentCompiledScript;

    // Version counters: an async pass applies its result only if the document (and the editor's
    // environment) still stand where they were when the pass began.
    private int _documentVersion;
    private int _colouredDocumentVersion = -1;
    private int _analysedDocumentVersion = -1;
    private int _editorStateVersion;

    // The document version most recently handed to the manager, so CompileAsync only refreshes
    // the manager's document (which discards its compile cache) when the text has moved on.
    private int _managerDocumentVersion = -1;

    private CancellationTokenSource? _syntacticCts;
    private CancellationTokenSource? _analysisCts;
    private bool _syntacticPassInProgress;
    private bool _analysisInProgress;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<DiagnosticsUpdatedEventArgs>? DiagnosticsUpdated;

    /// <inheritdoc/>
    public event EventHandler? ScriptChanged;

    /// <summary>
    /// Initialises a new instance of the <see cref="AvaloniaScriptEditor"/> class.
    /// </summary>
    public AvaloniaScriptEditor()
    {
        _textEditor = new TextEditor
        {
            ShowLineNumbers = true,
            FontFamily = new FontFamily("Cascadia Code, Cascadia Mono, Menlo, Consolas, monospace"),
            FontSize = 14,
        };

        _textEditor.Options.ConvertTabsToSpaces = true;
        _textEditor.Options.IndentationSize = 4;
        _textEditor.Options.HighlightCurrentLine = true;

        _diagnosticRenderer = new DiagnosticRenderer(_textEditor.Document);

        var textView = _textEditor.TextArea.TextView;
        textView.LineTransformers.Add(_colorizer);
        textView.BackgroundRenderers.Add(_diagnosticRenderer);

        _textEditor.TextChanged += OnTextChanged;

        _syntacticTimer = new DispatcherTimer { Interval = SyntacticPassDelay };
        _syntacticTimer.Tick += OnSyntacticTimerTick;

        _fullPassTimer = new DispatcherTimer { Interval = FullPassDelay };
        _fullPassTimer.Tick += OnFullPassTimerTick;

        InitialiseCompletion();
        InitialiseCallTips();
        InitialiseHover();
        InitialiseHelp();

        ActualThemeVariantChanged += (_, _) => ApplyTheme();

        Content = _textEditor;
        ApplyTheme();
    }

    // ── IScriptEditor ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public ScriptEnvironment? Environment
    {
        get => _environment;
        set
        {
            ThrowIfDisposed();

            _editorStateVersion++;
            CancelPendingPasses();
            _manager?.Dispose();
            _environment = value;
            _manager = value is null ? null : new EditorManager(value);
            _currentCompiledScript = null;
            _managerDocumentVersion = -1;

            // Force both passes to rerun against the new environment.
            _colouredDocumentVersion = -1;
            _analysedDocumentVersion = -1;

            if (_manager is not null)
            {
                Restart(_syntacticTimer);
                Restart(_fullPassTimer);
            }
        }
    }

    /// <inheritdoc/>
    public string Script
    {
        get => _textEditor.Text;
        set
        {
            ThrowIfDisposed();
            CloseCompletionWindow();
            CancelCompletion();
            CancelCallTip();
            _textEditor.Text = value ?? string.Empty;
        }
    }

    /// <inheritdoc/>
    public bool HasErrors => _currentDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <inheritdoc/>
    public IReadOnlyList<Diagnostic> CurrentDiagnostics => _currentDiagnostics;

    /// <inheritdoc/>
    public ExecutableScript? CurrentCompiledScript => _currentCompiledScript;

    /// <inheritdoc/>
    public EditorManager? Manager => _manager;

    /// <summary>
    /// Compiles the current script and returns the result.
    /// </summary>
    /// <returns>The compiled script, ready to run.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Environment"/> has not been set.</exception>
    public Task<ExecutableScript> CompileAsync()
    {
        return CompileAsync(CancellationToken.None);
    }

    /// <summary>
    /// Compiles the current script and returns the result.
    /// </summary>
    /// <param name="cancellationToken">A token that abandons the compilation.</param>
    /// <returns>The compiled script, ready to run.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Environment"/> has not been set.</exception>
    public async Task<ExecutableScript> CompileAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_manager is null)
        {
            throw new InvalidOperationException($"{nameof(Environment)} must be set before compiling.");
        }

        // The manager compiles its own document, which the debounced passes may not have caught
        // up with yet; bring it level with the text on screen first. No ConfigureAwait(false)
        // here: the manager is single-threaded and the next call must stay on the UI thread.
        await EnsureManagerDocumentCurrentAsync(cancellationToken);

        _currentCompiledScript = await _manager.CompileAsync(cancellationToken).ConfigureAwait(false);
        return _currentCompiledScript;
    }

    // ── Theming ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets an explicit colour theme for the editor surface. When <see langword="null"/>
    /// (the default), the editor follows the Avalonia theme variant it is displayed under, using
    /// <see cref="EditorTheme.Dark"/> for a dark variant and <see cref="EditorTheme.Light"/> otherwise.
    /// </summary>
    public EditorTheme? ColorTheme
    {
        get => _theme;
        set
        {
            _theme = value;
            ApplyTheme();
        }
    }

    /// <summary>
    /// Gets the theme currently applied to the editor surface: <see cref="ColorTheme"/> if set,
    /// otherwise the theme matching the active Avalonia theme variant.
    /// </summary>
    public EditorTheme EffectiveTheme =>
        _theme ?? (ActualThemeVariant == ThemeVariant.Dark ? EditorTheme.Dark : EditorTheme.Light);

    private void ApplyTheme()
    {
        var theme = EffectiveTheme;
        var textArea = _textEditor.TextArea;
        var textView = textArea.TextView;

        _textEditor.Background = theme.Background.ToBrush();
        _textEditor.Foreground = theme.Foreground.ToBrush();
        _textEditor.LineNumbersForeground = theme.FoldMarginForeground.ToBrush();

        textArea.CaretBrush = theme.Foreground.ToBrush();
        textArea.SelectionBrush = theme.SelectionBackground.ToBrush();
        textArea.SelectionForeground = theme.SelectionForeground.ToBrush();
        textArea.SelectionBorder = null;

        textView.CurrentLineBackground = theme.CaretLineBackground.ToBrush();
        textView.CurrentLineBorder = new ImmutablePen(theme.CaretLineBackground.ToBrush());

        _colorizer.SetTheme(theme);
        ApplyHoverTheme(theme);
        _diagnosticRenderer.SetTheme(theme);

        if (_completionWindow is not null)
        {
            ApplyCompletionTheme(_completionWindow, theme);
        }

        if (_callTipWindow is not null)
        {
            ApplyCallTipTheme(_callTipWindow, theme);
        }

        textView.Redraw();
    }

    // ── Live analysis ────────────────────────────────────────────────────────

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        _documentVersion++;
        _currentCompiledScript = null;

        // Whatever the hover popup describes may no longer be there.
        CloseHover();

        Restart(_syntacticTimer);
        Restart(_fullPassTimer);
    }

    private async void OnSyntacticTimerTick(object? sender, EventArgs e)
    {
        _syntacticTimer.Stop();

        if (_colouredDocumentVersion != _documentVersion)
        {
            await PerformSyntacticPassAsync();
        }
    }

    private async void OnFullPassTimerTick(object? sender, EventArgs e)
    {
        _fullPassTimer.Stop();

        if (_analysedDocumentVersion != _documentVersion)
        {
            await PerformFullPassAsync();
        }
    }

    /// <summary>
    /// Runs the cheap syntax-only classification pass and repaints colour from it.
    /// </summary>
    private async Task PerformSyntacticPassAsync()
    {
        // Skip while the full pass runs: it is about to produce strictly better colouring, and
        // both mutating the manager's context concurrently is unsafe. The finally block re-queues.
        if (_manager is null || _disposed || _syntacticPassInProgress || _analysisInProgress)
        {
            return;
        }

        var manager = _manager;
        var stateVersion = _editorStateVersion;
        var documentVersion = _documentVersion;
        var script = Script;

        CancelAndDispose(ref _syntacticCts);
        _syntacticCts = new CancellationTokenSource();
        var ct = _syntacticCts.Token;

        _syntacticPassInProgress = true;
        _managerDocumentVersion = documentVersion;

        try
        {
            var result = await manager.ApplySyntacticPassAsync(script, ct);

            if (!IsCurrent(documentVersion, stateVersion) || _analysedDocumentVersion == documentVersion)
            {
                return;
            }

            _colorizer.SetClassifications(result.Classifications);
            _textEditor.TextArea.TextView.Redraw();
            _colouredDocumentVersion = documentVersion;
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException) when (!IsCurrent(documentVersion, stateVersion))
        {
        }
        finally
        {
            _syntacticPassInProgress = false;

            if (!_disposed && _colouredDocumentVersion != _documentVersion && !_syntacticTimer.IsEnabled)
            {
                _syntacticTimer.Start();
            }
        }
    }

    /// <summary>
    /// Runs the full analysis pass, then applies diagnostics and semantic colouring and raises events.
    /// </summary>
    private async Task PerformFullPassAsync()
    {
        if (_manager is null || _disposed || _analysisInProgress)
        {
            return;
        }

        var manager = _manager;
        var stateVersion = _editorStateVersion;
        var documentVersion = _documentVersion;
        var script = Script;

        CancelAndDispose(ref _analysisCts);
        _analysisCts = new CancellationTokenSource();
        var ct = _analysisCts.Token;

        _analysisInProgress = true;
        _managerDocumentVersion = documentVersion;

        try
        {
            // Runs on the thread pool inside EditorManager; resumes here on the UI thread.
            await manager.ApplyScript(script, ct);

            if (!IsCurrent(documentVersion, stateVersion))
            {
                return;
            }

            _currentDiagnostics = manager.LastDiagnostics;
            _analysedDocumentVersion = documentVersion;
            _colouredDocumentVersion = documentVersion;

            _colorizer.SetClassifications(manager.LastClassifications);
            _diagnosticRenderer.SetDiagnostics(_currentDiagnostics, _textEditor.Document.TextLength);
            _textEditor.TextArea.TextView.Redraw();

            DiagnosticsUpdated?.Invoke(this, new DiagnosticsUpdatedEventArgs(_currentDiagnostics));
            ScriptChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException) when (!IsCurrent(documentVersion, stateVersion))
        {
        }
        finally
        {
            _analysisInProgress = false;

            // A superseded pass leaves the document unanalysed, so queue another one.
            if (!_disposed && _analysedDocumentVersion != _documentVersion && !_fullPassTimer.IsEnabled)
            {
                _fullPassTimer.Start();
            }
        }
    }

    private bool IsCurrent(int documentVersion, int stateVersion)
    {
        return !_disposed && documentVersion == _documentVersion && stateVersion == _editorStateVersion;
    }

    // ── Lifetime ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Stops live analysis and releases the Roslyn workspace.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _editorStateVersion++;

        _syntacticTimer.Stop();
        _fullPassTimer.Stop();
        CancelPendingPasses();

        _textEditor.TextChanged -= OnTextChanged;
        DetachHover();
        DetachCompletion();
        DetachCallTips();
        DetachHelp();

        _manager?.Dispose();
        _manager = null;

        GC.SuppressFinalize(this);
    }

    private void CancelPendingPasses()
    {
        CancelAndDispose(ref _syntacticCts);
        CancelAndDispose(ref _analysisCts);

        _completionTimer.Stop();
        CancelAndDispose(ref _completionCts);
        CloseCompletionWindow();

        CancelCallTip();
        CloseHover();
    }

    private static void CancelAndDispose(ref CancellationTokenSource? cts)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private static void Restart(DispatcherTimer timer)
    {
        timer.Stop();
        timer.Start();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AvaloniaScriptEditor));
        }
    }
}
