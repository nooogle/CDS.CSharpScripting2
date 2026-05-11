using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.ComponentModel;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// Provides a Scintilla-based script editor with live diagnostics, classifications, and editor assistance.
/// </summary>
public partial class ScintillaScriptEditor : UserControl, Editors.IScriptEditor
{
    private const string CDSCategory = "CDS";

    private const int ScintillaErrorIndicatorIndex = 3;
    private const int ScintillaWarningIndicatorIndex = 4;
    private const int ScintillaHighlightIndicatorIndex = 5;

    private readonly ImmutableDictionary<Classification.SymbolClassification, int> _classificationKindToScintillaStyle;
    private ImmutableArray<Diagnostic> _currentDiagnostics = [];
    private ExecutableScript? _currentCompiledScript;
    private Editors.EditorManager? _manager;
    private ScriptEnvironment? _environment;
    private string _lastScript = "";
    private bool _analysisInProgress;
    private bool _disposed;
    private int _editorStateVersion;
    private CancellationTokenSource? _completionCts;

    private readonly ToolTipDiagnostics _diagnosticsToolTipManager;
    private readonly FormAPIInfo _apiInfoForm = new();
    private readonly Classification.Coloriser _coloriser = new();

    private CallTipSession? _callTipSession;
    private CancellationTokenSource? _dwellCts;

    // ── IScriptEditor ────────────────────────────────────────────────────────

    [Category(CDSCategory)]
    public event EventHandler<Editors.DiagnosticsUpdatedEventArgs>? DiagnosticsUpdated;

    [Category(CDSCategory)]
    public event EventHandler? ScriptChanged;

    /// <inheritdoc/>
    public Editors.EditorManager? Manager => _manager;

    /// <inheritdoc/>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ScriptEnvironment? Environment
    {
        get => _environment;
        set
        {
            _editorStateVersion++;
            CancelPendingAsyncOperations();
            _manager?.Dispose();
            _environment = value;
            _manager = value is null ? null : new Editors.EditorManager(value);
            ResetAnalysisState();
        }
    }

    /// <inheritdoc/>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Script
    {
        get => TryGetScript(out var script)
            ? script
            : throw new ObjectDisposedException(nameof(ScintillaScriptEditor));
        set
        {
            ThrowIfEditorUnavailable();
            scintilla.Text = value;
        }
    }

    /// <inheritdoc/>
    public bool HasErrors =>
        _currentDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <inheritdoc/>
    public IReadOnlyList<Diagnostic> CurrentDiagnostics => _currentDiagnostics;

    /// <inheritdoc/>
    public ExecutableScript? CurrentCompiledScript => _currentCompiledScript;

    /// <inheritdoc/>
    public async Task<ExecutableScript> CompileAsync(CancellationToken cancellationToken = default)
    {
        if (_manager is null)
            throw new InvalidOperationException($"{nameof(Environment)} must be set before compiling.");

        _currentCompiledScript = await _manager.CompileAsync(cancellationToken).ConfigureAwait(false);
        return _currentCompiledScript;
    }

    // ── Construction ─────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="ScintillaScriptEditor"/> class.
    /// </summary>
    public ScintillaScriptEditor()
    {
        InitializeComponent();

        _diagnosticsToolTipManager = new ToolTipDiagnostics(scintilla, toolTip);

        var builder = new Dictionary<Classification.SymbolClassification, int>();
        var names = (Classification.SymbolClassification[])Enum.GetValues(typeof(Classification.SymbolClassification));

        for (int i = 1; i <= names.Length; i++)
        {
            builder[names[i - 1]] = i;
        }

        _classificationKindToScintillaStyle = builder.ToImmutableDictionary();

        InitialiseScintilla();
    }

    /// <summary>
    /// Initializes editor UI settings when the control is loaded.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (DesignMode)
        {
            return;
        }

        timerChangeMonitor.Start();

        scintilla.Margins[0].Type = ScintillaNET.MarginType.Number;
        scintilla.Margins[0].Width = 40;

        scintilla.Margins[1].Type = ScintillaNET.MarginType.Symbol;
        scintilla.Margins[1].Width = 8;
        scintilla.Margins[1].Sensitive = false;
        scintilla.Margins[1].Mask = 0;
    }

    /// <summary>
    /// Configures Scintilla styling, indicators, and hover behavior.
    /// </summary>
    private void InitialiseScintilla()
    {
        scintilla.Styles[ScintillaNET.Style.Default].Font = "Cascadia Mono";
        scintilla.Styles[ScintillaNET.Style.Default].SizeF = 10;
        scintilla.StyleClearAll();

        scintilla.MouseDwellTime = 500;

        scintilla.AutoCIgnoreCase = true;
        scintilla.AutoCOrder = ScintillaNET.Order.Custom;
        scintilla.AutoCMaxHeight = 12;

        foreach (var (classificationName, styleIndex) in _classificationKindToScintillaStyle)
        {
            var colorScheme = _coloriser.FromClassificationName(classificationName);
            scintilla.Styles[styleIndex].ForeColor = colorScheme.Foreground;
            scintilla.Styles[styleIndex].BackColor = colorScheme.Background;
            scintilla.Styles[styleIndex].Bold = colorScheme.Bold;
            scintilla.Styles[styleIndex].Italic = colorScheme.Italics;
        }

        scintilla.Indicators[ScintillaErrorIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Squiggle;
        scintilla.Indicators[ScintillaErrorIndicatorIndex].ForeColor = Color.Red;

        scintilla.Indicators[ScintillaWarningIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Squiggle;
        scintilla.Indicators[ScintillaWarningIndicatorIndex].ForeColor = Color.Green;

        scintilla.Indicators[ScintillaHighlightIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Box;
    }

    // ── Internal analysis cycle ───────────────────────────────────────────────

    /// <summary>
    /// Resets cached diagnostics, compilation state, and transient editor UI.
    /// </summary>
    private void ResetAnalysisState()
    {
        _currentDiagnostics = [];
        _currentCompiledScript = null;
        _lastScript = string.Empty;

        CancelAndDispose(ref _dwellCts);

        if (!CanAccessEditor)
        {
            return;
        }

        _diagnosticsToolTipManager.ClearHover();
        ClearWarningAndErrorIndicators();
        _apiInfoForm.Hide();
    }

    /// <summary>
    /// Handles text edits by clearing cached analysis and restarting the debounce timer.
    /// </summary>
    private void HandleTextChanged()
    {
        if (!CanAccessEditor)
        {
            return;
        }

        ResetAnalysisState();

        timerChangeMonitor.Stop();
        timerChangeMonitor.Start();
    }

    /// <summary>
    /// Starts a fresh live-analysis pass once the debounce timer elapses.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void timerChangeMonitor_Tick(object sender, EventArgs e)
    {
        timerChangeMonitor.Stop();

        if (TryGetScript(out var script) && _lastScript != script)
        {
            await PerformLiveAnalysisAsync();
        }
    }

    /// <summary>
    /// Performs a live-analysis pass and updates diagnostics, classifications, and events.
    /// </summary>
    private async Task PerformLiveAnalysisAsync()
    {
        if (_manager is null || _analysisInProgress || !TryGetScript(out var scriptSnapshot))
        {
            return;
        }

        var manager = _manager;
        var stateVersion = _editorStateVersion;

        _analysisInProgress = true;

        try
        {
            ClearWarningAndErrorIndicators();

            await manager.ApplyScript(scriptSnapshot);

            if (stateVersion != _editorStateVersion ||
                !ReferenceEquals(manager, _manager) ||
                !TryGetScript(out var currentScript))
            {
                return;
            }

            if (!string.Equals(scriptSnapshot, currentScript, StringComparison.Ordinal))
            {
                return;
            }

            _currentDiagnostics = manager.LastDiagnostics;
            _lastScript = scriptSnapshot;

            ApplyDiagnosticsToEditor(_currentDiagnostics);
            ApplyClassificationsToEditor(manager.LastClassifications);

            if (CanAccessEditor)
            {
                DiagnosticsUpdated?.Invoke(this, new Editors.DiagnosticsUpdatedEventArgs(_currentDiagnostics));
                ScriptChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
        }
        finally
        {
            _analysisInProgress = false;

            if (stateVersion == _editorStateVersion &&
                TryGetScript(out var currentScript) &&
                _lastScript != currentScript &&
                !timerChangeMonitor.Enabled)
            {
                timerChangeMonitor.Start();
            }
        }
    }

    // ── Visual feedback ───────────────────────────────────────────────────────

    /// <summary>
    /// Applies diagnostic indicators to the editor for source-based warnings and errors.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to render.</param>
    private void ApplyDiagnosticsToEditor(ImmutableArray<Diagnostic> diagnostics)
    {
        if (!CanAccessEditor)
        {
            return;
        }

        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Location.IsInSource &&
                diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            {
                MarkDiagnosticInEditor(diagnostic);
            }
        }
    }

    /// <summary>
    /// Marks a single diagnostic in the editor using the configured indicator styles.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to render.</param>
    private void MarkDiagnosticInEditor(Diagnostic diagnostic)
    {
        if (!TryGetScript(out var script))
        {
            return;
        }

        scintilla.IndicatorCurrent =
            diagnostic.Severity == DiagnosticSeverity.Error
            ? ScintillaErrorIndicatorIndex
            : ScintillaWarningIndicatorIndex;

        var start = diagnostic.Location.SourceSpan.Start;
        var length = diagnostic.Location.SourceSpan.Length;

        if (length == 0)
        {
            start = Math.Max(0, start - 1);
            length = 1;
        }

        var documentLength = script.Length;

        if (!TryGetDocumentRange(start, length, documentLength, out var boundedStart, out var boundedLength))
        {
            return;
        }

        scintilla.IndicatorFillRange(position: boundedStart, length: boundedLength);
    }

    /// <summary>
    /// Applies syntax classification styling to the editor.
    /// </summary>
    /// <param name="classifications">The classifications to apply.</param>
    private void ApplyClassificationsToEditor(IReadOnlyList<Classification.ClassifiedSymbol> classifications)
    {
        if (!TryGetScript(out var script))
        {
            return;
        }

        var documentLength = script.Length;

        scintilla.StartStyling(0);
        scintilla.SetStyling(documentLength, 0);

        foreach (var classification in classifications)
        {
            if (_classificationKindToScintillaStyle.TryGetValue(classification.Classification, out var styleIndex)
                && TryGetDocumentRange(
                    classification.SpanStart,
                    classification.SpanLength,
                    documentLength,
                    out var boundedStart,
                    out var boundedLength))
            {
                scintilla.StartStyling(boundedStart);
                scintilla.SetStyling(boundedLength, styleIndex);
            }
        }
    }

    /// <summary>
    /// Clears all warning and error indicators from the editor.
    /// </summary>
    private void ClearWarningAndErrorIndicators()
    {
        if (!TryGetScript(out var script))
        {
            return;
        }

        scintilla.IndicatorCurrent = ScintillaErrorIndicatorIndex;
        scintilla.IndicatorClearRange(0, script.Length);

        scintilla.IndicatorCurrent = ScintillaWarningIndicatorIndex;
        scintilla.IndicatorClearRange(0, script.Length);
    }

    // ── Highlight API (public — used by ClassifiedSpans and SyntaxTree demos) ─

    /// <summary>
    /// Highlights the specified text range in the editor.
    /// </summary>
    /// <param name="start">The zero-based start position.</param>
    /// <param name="length">The length of the range to highlight.</param>
    public void HighlightText(int start, int length)
    {
        if (!TryGetScript(out var script))
        {
            return;
        }

        ClearHighlightText();

        var documentLength = script.Length;

        if (!TryGetDocumentRange(start, length, documentLength, out var boundedStart, out var boundedLength))
        {
            return;
        }

        scintilla.IndicatorCurrent = ScintillaHighlightIndicatorIndex;
        scintilla.IndicatorFillRange(position: boundedStart, length: boundedLength);
        scintilla.ScrollCaret();
    }

    /// <summary>
    /// Clamps a requested editor span to the current document bounds.
    /// </summary>
    /// <param name="start">The requested zero-based start position.</param>
    /// <param name="length">The requested span length.</param>
    /// <param name="documentLength">The current document length.</param>
    /// <param name="boundedStart">The bounded start position.</param>
    /// <param name="boundedLength">The bounded span length.</param>
    /// <returns><see langword="true"/> when a non-empty in-range span is available; otherwise <see langword="false"/>.</returns>
    private static bool TryGetDocumentRange(
        int start,
        int length,
        int documentLength,
        out int boundedStart,
        out int boundedLength)
    {
        boundedStart = Math.Clamp(start, 0, documentLength);
        boundedLength = Math.Min(length, documentLength - boundedStart);

        return boundedLength > 0;
    }

    /// <summary>
    /// Clears any active highlight range from the editor.
    /// </summary>
    public void ClearHighlightText()
    {
        if (!TryGetScript(out var script))
        {
            return;
        }

        scintilla.IndicatorCurrent = ScintillaHighlightIndicatorIndex;
        scintilla.IndicatorClearRange(0, script.Length);
    }

    // ── Scintilla event handlers ──────────────────────────────────────────────

    /// <summary>
    /// Handles character insertion events from Scintilla.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_CharAdded(object sender, ScintillaNET.CharAddedEventArgs e)
    {
        HandleTextChanged();

        var ch = (char)e.Char;

        if (ch == '.')
        {
            // Member access — cancel any open session and immediately start a fresh one.
            scintilla.AutoCCancel();
            StartCompletionSession(immediate: true);
        }
        else if (ch == '(')
        {
            scintilla.AutoCCancel();
            _completionCts?.Cancel();
            _ = StartCallTipSessionAsync();
        }
        else if (ch == ',')
        {
            if (_callTipSession is not null)
                _ = UpdateCallTipArgumentAsync();
            else
                _ = StartCallTipSessionAsync();  // re-activate if the outer session was lost
        }
        else if (ch == ')')
        {
            _callTipSession?.Cancel();
            _callTipSession = null;
            _ = StartCallTipSessionAsync();  // restore the enclosing call's tip if one exists
        }
        else if (!scintilla.AutoCActive && (char.IsLetter(ch) || ch == '_'))
        {
            // First identifier character of a new word — trigger after a short delay so
            // rapid typists don't fire a Roslyn request on every single keystroke.
            StartCompletionSession(immediate: false);
        }
        else if (scintilla.AutoCActive && !char.IsLetterOrDigit(ch) && ch != '_')
        {
            // Non-identifier character while the list is open — dismiss.
            scintilla.AutoCCancel();
            _completionCts?.Cancel();
        }
    }

    /// <summary>
    /// Handles character deletion events from Scintilla.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_Delete(object sender, ScintillaNET.ModificationEventArgs e) =>
        HandleTextChanged();

    /// <summary>
    /// Handles mouse movement over the editor.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_MouseMove(object sender, MouseEventArgs e)
    {
        // Reserved for future pointer-tracking features.
    }

    /// <summary>
    /// Handles clicks on the up/down arrow buttons embedded in an active call tip.
    /// </summary>
    private void scintilla_CallTipClick(object sender, ScintillaNET.CallTipClickEventArgs e)
    {
        if (_callTipSession is null)
            return;

        if (e.CallTipClickType == ScintillaNET.CallTipClickType.UpArrow)
            _callTipSession.PreviousOverload();
        else if (e.CallTipClickType == ScintillaNET.CallTipClickType.DownArrow)
            _callTipSession.NextOverload();
    }

    /// <summary>
    /// Handles the start of a dwell operation to show hover diagnostics.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_DwellStart(object sender, ScintillaNET.DwellEventArgs e)
    {
        CancelAndDispose(ref _dwellCts);
        _dwellCts = new CancellationTokenSource();
        _ = HandleDwellAsync(e.Position, _dwellCts.Token);
    }

    /// <summary>
    /// Handles the end of a dwell operation to clear hover tooltips.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_DwellEnd(object sender, ScintillaNET.DwellEventArgs e)
    {
        CancelAndDispose(ref _dwellCts);
        _diagnosticsToolTipManager.HandleDwellEnd();
    }

    /// <summary>
    /// Fetches API info asynchronously then shows the combined hover tooltip.
    /// Runs on the UI thread throughout; no marshal-back needed.
    /// </summary>
    private async Task HandleDwellAsync(int position, CancellationToken ct)
    {
        APIInfo.APIInfoResult? apiInfo = null;
        var manager = _manager;
        var stateVersion = _editorStateVersion;

        if (manager is not null)
        {
            try
            {
                apiInfo = await manager.GetAPIInfo(position);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException) when (
                ct.IsCancellationRequested ||
                stateVersion != _editorStateVersion ||
                !ReferenceEquals(manager, _manager) ||
                !CanAccessEditor)
            {
                return;
            }
        }

        if (ct.IsCancellationRequested ||
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
            return;

        _diagnosticsToolTipManager.HandleDwellStart(_currentDiagnostics, position, apiInfo);
    }

    // ── Code completion ───────────────────────────────────────────────────────

    /// <summary>
    /// Cancels any pending completion request and starts a new one.
    /// </summary>
    /// <param name="immediate">When <see langword="false"/> a 150 ms debounce is applied so rapid
    /// typing only fires one Roslyn request per word, not one per character.</param>
    private void StartCompletionSession(bool immediate)
    {
        CancelAndDispose(ref _completionCts);
        _completionCts = new CancellationTokenSource();
        _ = ShowCompletionAsync(_completionCts.Token, immediate);
    }

    /// <summary>
    /// Fetches completions from Roslyn and populates the Scintilla autocomplete list.
    /// Runs on the UI thread throughout; the debounce <see cref="Task.Delay"/> simply
    /// yields control without leaving the <see cref="System.Windows.Forms.WindowsFormsSynchronizationContext"/>.
    /// </summary>
    private async Task ShowCompletionAsync(CancellationToken cancellationToken, bool immediate)
    {
        try
        {
            if (!immediate)
                await Task.Delay(150, cancellationToken);

            var manager = _manager;
            var stateVersion = _editorStateVersion;

            if (cancellationToken.IsCancellationRequested ||
                manager is null ||
                !TryGetScript(out var script))
                return;

            // Keep the Roslyn document current without paying for a full diagnostics pass.
            await manager.UpdateScriptDocumentAsync(script);

            if (cancellationToken.IsCancellationRequested ||
                stateVersion != _editorStateVersion ||
                !ReferenceEquals(manager, _manager) ||
                !TryGetCurrentPosition(out var currentPosition))
                return;

            int wordStart = scintilla.WordStartPosition(currentPosition, onlyWordCharacters: true);
            int lenEntered = currentPosition - wordStart;

            var completions = await manager.GetAutoCompletions(currentPosition);

            if (cancellationToken.IsCancellationRequested ||
                stateVersion != _editorStateVersion ||
                !ReferenceEquals(manager, _manager) ||
                !CanAccessEditor)
                return;

            if (!completions.Any())
            {
                scintilla.AutoCCancel();
                return;
            }

            var list = string.Join(
                scintilla.AutoCSeparator.ToString(),
                completions.Select(c => c.DisplayText));

            scintilla.AutoCShow(lenEntered, list);
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested || !CanAccessEditor) { }
    }

    /// <summary>
    /// Handles key input for editor assistance features.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void scintilla_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && e.Control && e.Shift)
        {
            _ = StartCallTipSessionAsync();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Space && e.Control)
        {
            TryRunAutoComplete();
        }
        else if (e.KeyCode == Keys.F1)
        {
            if (_manager is null || !TryGetCurrentPosition(out var pos))
            {
                return;
            }

            var point = new Point(
                x: scintilla.PointXFromPosition(pos),
                y: scintilla.PointYFromPosition(pos));

            var manager = _manager;
            var stateVersion = _editorStateVersion;

            APIInfo.APIInfoResult? apiInfo;

            try
            {
                apiInfo = await manager.GetAPIInfo(pos);
            }
            catch (ObjectDisposedException) when (
                stateVersion != _editorStateVersion ||
                !ReferenceEquals(manager, _manager) ||
                !CanAccessEditor)
            {
                return;
            }

            if (stateVersion != _editorStateVersion ||
                !ReferenceEquals(manager, _manager) ||
                !CanAccessEditor)
            {
                return;
            }

            _apiInfoForm.ShowAPIInfo(parent: this, location: point, apiInfo: apiInfo);
        }
        else if (e.KeyCode == Keys.Escape)
        {
            scintilla.AutoCCancel();
            _callTipSession?.Cancel();
            _callTipSession = null;
            _apiInfoForm.Hide();
        }
        else if (_callTipSession is not null && !e.Control && !e.Alt)
        {
            if (e.KeyCode == Keys.Up)
            {
                _callTipSession.PreviousOverload();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                _callTipSession.NextOverload();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
    }

    /// <summary>
    /// Shows the autocomplete list at the current caret position (explicit Ctrl+Space trigger).
    /// </summary>
    private void TryRunAutoComplete()
    {
        scintilla.AutoCCancel();
        StartCompletionSession(immediate: true);
    }

    // ── Call tips ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts a new call tip session when the cursor has just entered a method argument list.
    /// Cancels any session already in progress.
    /// </summary>
    private async Task StartCallTipSessionAsync()
    {
        if (_manager is null)
        {
            return;
        }

        _callTipSession?.Cancel();
        _callTipSession = null;

        var manager = _manager;
        var stateVersion = _editorStateVersion;

        if (!TryGetScript(out var script))
        {
            return;
        }

        try
        {
            await manager.UpdateScriptDocumentAsync(script);
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
            return;
        }

        if (stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !TryGetCurrentPosition(out var pos))
        {
            return;
        }

        APIInfo.CallTipContext? context;

        try
        {
            context = await manager.GetCallTipContext(pos);
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
            return;
        }

        if (context is null ||
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
            return;

        // API info is resolved at the character just before '(' to land on the method name.
        APIInfo.APIInfoResult? apiInfo;

        try
        {
            apiInfo = await manager.GetAPIInfo(Math.Max(0, context.OpenParenPosition - 1));
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
            return;
        }

        if (apiInfo?.MemberInfos is null ||
            apiInfo.MemberInfos.Count == 0 ||
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
            return;

        _callTipSession = new CallTipSession(
            scintilla,
            apiInfo.MemberInfos,
            context.OpenParenPosition,
            context.ArgumentIndex);
    }

    /// <summary>
    /// Updates the active parameter highlight when the cursor moves to a different argument.
    /// </summary>
    private async Task UpdateCallTipArgumentAsync()
    {
        if (_manager is null || _callTipSession is null)
        {
            return;
        }

        var manager = _manager;
        var callTipSession = _callTipSession;
        var stateVersion = _editorStateVersion;

        if (!TryGetScript(out var script))
        {
            return;
        }

        try
        {
            await manager.UpdateScriptDocumentAsync(script);
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
            return;
        }

        if (stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !ReferenceEquals(callTipSession, _callTipSession) ||
            !TryGetCurrentPosition(out var currentPosition))
        {
            return;
        }

        APIInfo.CallTipContext? context;

        try
        {
            context = await manager.GetCallTipContext(currentPosition);
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
            return;
        }

        if (context is null)
        {
            callTipSession.Cancel();
            _callTipSession = null;
            return;
        }

        if (stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !ReferenceEquals(callTipSession, _callTipSession) ||
            !CanAccessEditor)
        {
            return;
        }

        callTipSession.UpdateArgument(context.ArgumentIndex);
    }

    /// <summary>
    /// Handles the cancellation of the autocomplete list.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_AutoCCancelled(object sender, EventArgs e) { }

    /// <summary>
    /// Handles deletion while the autocomplete list is active.
    /// Scintilla dismisses the list when the user backspaces past the opening prefix;
    /// this re-triggers completion if the caret is still inside a word.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_AutoCCharDeleted(object sender, EventArgs e)
    {
        if (!TryGetCurrentPosition(out var pos))
        {
            return;
        }

        int wordStart = scintilla.WordStartPosition(pos, onlyWordCharacters: true);
        if (pos > wordStart)
        {
            StartCompletionSession(immediate: true);
        }
    }

    /// <summary>
    /// Handles completion selection from the autocomplete list.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_AutoCCompleted(object sender, ScintillaNET.AutoCSelectionEventArgs e) { }

    /// <summary>
    /// Gets a value indicating whether the Scintilla editor can still be accessed safely.
    /// </summary>
    private bool CanAccessEditor =>
        !_disposed &&
        !IsDisposed &&
        !Disposing &&
        !scintilla.IsDisposed;

    /// <summary>
    /// Cancels pending asynchronous editor interactions that may resume after disposal or environment changes.
    /// </summary>
    private void CancelPendingAsyncOperations()
    {
        CancelAndDispose(ref _completionCts);
        CancelAndDispose(ref _dwellCts);
        _callTipSession?.Cancel();
        _callTipSession = null;

        if (CanAccessEditor)
        {
            _apiInfoForm.Hide();
            _diagnosticsToolTipManager.ClearHover();
        }
    }

    /// <summary>
    /// Cancels and disposes the specified token source.
    /// </summary>
    /// <param name="cts">The token source to cancel and dispose.</param>
    private static void CancelAndDispose(ref CancellationTokenSource? cts)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    /// <summary>
    /// Throws when the editor control or its Scintilla child has already been disposed.
    /// </summary>
    private void ThrowIfEditorUnavailable()
    {
        if (!CanAccessEditor)
        {
            throw new ObjectDisposedException(nameof(ScintillaScriptEditor));
        }
    }

    /// <summary>
    /// Tries to read the current script text without touching a disposed Scintilla control.
    /// </summary>
    /// <param name="script">The current script when available.</param>
    /// <returns><see langword="true"/> when the script was read; otherwise <see langword="false"/>.</returns>
    private bool TryGetScript(out string script)
    {
        if (!CanAccessEditor)
        {
            script = string.Empty;
            return false;
        }

        script = scintilla.Text;
        return true;
    }

    /// <summary>
    /// Tries to read the current caret position without touching a disposed Scintilla control.
    /// </summary>
    /// <param name="currentPosition">The current caret position when available.</param>
    /// <returns><see langword="true"/> when the caret position was read; otherwise <see langword="false"/>.</returns>
    private bool TryGetCurrentPosition(out int currentPosition)
    {
        if (!CanAccessEditor)
        {
            currentPosition = 0;
            return false;
        }

        currentPosition = scintilla.CurrentPosition;
        return true;
    }
}
