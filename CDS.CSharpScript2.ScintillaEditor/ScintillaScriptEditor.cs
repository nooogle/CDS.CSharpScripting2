using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.ComponentModel;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// Provides a Scintilla-based script editor with live diagnostics, syntax classifications,
/// completion lists, call tips, API information, and find/replace support.
/// </summary>
public partial class ScintillaScriptEditor : UserControl, Editors.IScriptEditor
{
    private const string CDSCategory = "CDS";

    private const int ScintillaErrorIndicatorIndex = 3;
    private const int ScintillaWarningIndicatorIndex = 4;
    private const int ScintillaHighlightIndicatorIndex = 5;

    private const int ScintillaDiagnosticsMarginIndex = 1;
    private const int ScintillaFoldMarginIndex = 2;

    // Marker numbers below Scintilla's reserved fold-marker range (25-31, see Marker.MaskFolders).
    private const int ScintillaErrorMarkerIndex = 0;
    private const int ScintillaWarningMarkerIndex = 1;

    // SC_FOLDLEVELBASE: Scintilla's zero-depth fold level. Line.FoldLevel is this value plus
    // nesting depth; the numeric part tops out at 4095, so depth is clamped defensively.
    private const int ScintillaFoldLevelBase = 1024;
    private const int ScintillaMaxFoldDepth = 4095 - ScintillaFoldLevelBase;

    private static readonly TimeSpan CommentChordTimeout = TimeSpan.FromSeconds(2);

    private readonly ImmutableDictionary<Classification.SymbolClassification, int> _classificationKindToScintillaStyle;
    private ImmutableArray<Diagnostic> _currentDiagnostics = [];
    private ExecutableScript? _currentCompiledScript;
    private Editors.EditorManager? _manager;
    private ScriptEnvironment? _environment;
    private bool _analysisInProgress;
    private bool _suppressTextChangeHandling;
    private bool _disposed;

    // Two counters, deliberately. _documentVersion tracks text edits and decides whether an
    // analysis result is still current. _editorStateVersion tracks the editor itself being
    // invalidated — environment swapped, control disposed. Merging them looks tempting but
    // breaks call tips: those guard on _editorStateVersion and would abandon their session on
    // every keystroke, which is precisely when the user is typing arguments.
    private long _documentVersion;
    private long _analysedDocumentVersion = -1;
    private long _colouredDocumentVersion = -1;
    private int _editorStateVersion;

    private CancellationTokenSource? _completionCts;
    private CancellationTokenSource? _analysisCts;
    private CancellationTokenSource? _syntacticCts;
    private bool _syntacticPassInProgress;

    private readonly ToolTipDiagnostics _diagnosticsToolTipManager;
    private readonly FormAPIInfo _apiInfoForm = new();
    private Classification.EditorTheme _theme = Classification.EditorTheme.Light;

    private CallTipSession? _callTipSession;
    private CancellationTokenSource? _callTipCts;
    private CancellationTokenSource? _dwellCts;
    private FormFindReplace? _findReplaceForm;
    private DateTime? _commentChordStartedAt;

    // ── IScriptEditor ────────────────────────────────────────────────────────

    /// <summary>Raised when the set of Roslyn diagnostics for the current script changes.</summary>
    [Category(CDSCategory)]
    public event EventHandler<Editors.DiagnosticsUpdatedEventArgs>? DiagnosticsUpdated;

    /// <summary>Raised when the text content of the script is modified by the user.</summary>
    [Category(CDSCategory)]
    public event EventHandler? ScriptChanged;

    private Editors.EditorManager? Manager => _manager;

    private ScriptEnvironment? Environment
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

            // The debounce timer stops once a document has been analysed, so without this a
            // host swapping the environment after load would get no fresh analysis until the
            // next keystroke — the script compiles against different references but keeps the
            // old squiggles.
            if (_manager is not null && CanAccessEditor && !DesignMode)
            {
                timerChangeMonitor.Stop();
                timerChangeMonitor.Start();
                timerSyntacticColour.Stop();
                timerSyntacticColour.Start();
            }
        }
    }

    private string Script
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

    private bool HasErrors =>
        _currentDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    private IReadOnlyList<Diagnostic> CurrentDiagnostics => _currentDiagnostics;

    private ExecutableScript? CurrentCompiledScript => _currentCompiledScript;

    private async Task<ExecutableScript> CompileAsync(CancellationToken cancellationToken = default)
    {
        if (_manager is null)
            throw new InvalidOperationException($"{nameof(Environment)} must be set before compiling.");

        _currentCompiledScript = await _manager.CompileAsync(cancellationToken).ConfigureAwait(false);
        return _currentCompiledScript;
    }

    // ── IScriptEditor (explicit implementations) ─────────────────────────────

    Editors.EditorManager? Editors.IScriptEditor.Manager => Manager;

    ScriptEnvironment? Editors.IScriptEditor.Environment
    {
        get => Environment;
        set => Environment = value;
    }

    string Editors.IScriptEditor.Script
    {
        get => Script;
        set => Script = value;
    }

    bool Editors.IScriptEditor.HasErrors => HasErrors;

    IReadOnlyList<Diagnostic> Editors.IScriptEditor.CurrentDiagnostics => CurrentDiagnostics;

    ExecutableScript? Editors.IScriptEditor.CurrentCompiledScript => CurrentCompiledScript;

    Task<ExecutableScript> Editors.IScriptEditor.CompileAsync(CancellationToken cancellationToken) =>
        CompileAsync(cancellationToken);

    // ── API facade ────────────────────────────────────────────────────────────

    /// <summary>Gets the custom API surface for this editor, grouping all script and display members under a single named property.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ScintillaScriptEditorApi API { get; }

    // ── Theming ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the color theme applied to the Scintilla surface — default text, caret line,
    /// selection, per-classification syntax colors, brace highlighting, diagnostic indicators,
    /// the fold margin, and the autocomplete list. Setting this re-applies every theme-dependent
    /// style immediately. The editor never follows the OS theme on its own; a host that wants
    /// that assigns <see cref="Classification.EditorTheme.Light"/> or
    /// <see cref="Classification.EditorTheme.Dark"/> here itself, e.g. in response to a system
    /// theme change notification.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Classification.EditorTheme Theme
    {
        get => _theme;
        set
        {
            _theme = value ?? throw new ArgumentNullException(nameof(value));
            ApplyTheme();
        }
    }

    // ── Construction ─────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="ScintillaScriptEditor"/> class.
    /// </summary>
    /// <remarks>
    /// Set <see cref="Editors.IScriptEditor.Environment"/> before compiling or relying on live analysis.
    /// </remarks>
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

        API = new ScintillaScriptEditorApi(this);
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
        timerSyntacticColour.Start();

        scintilla.Margins[0].Type = ScintillaNET.MarginType.Number;
        scintilla.Margins[0].Width = 40;

        scintilla.Margins[ScintillaDiagnosticsMarginIndex].Type = ScintillaNET.MarginType.Symbol;
        scintilla.Margins[ScintillaDiagnosticsMarginIndex].Width = 14;
        scintilla.Margins[ScintillaDiagnosticsMarginIndex].Sensitive = false;
        scintilla.Margins[ScintillaDiagnosticsMarginIndex].Mask =
            (1 << ScintillaErrorMarkerIndex) | (1 << ScintillaWarningMarkerIndex);

        scintilla.Margins[ScintillaFoldMarginIndex].Type = ScintillaNET.MarginType.Symbol;
        scintilla.Margins[ScintillaFoldMarginIndex].Mask = ScintillaNET.Marker.MaskFolders;
        scintilla.Margins[ScintillaFoldMarginIndex].Sensitive = true;
        scintilla.Margins[ScintillaFoldMarginIndex].Width = 16;

        // Show: Scintilla still auto-reveals a line hidden by a collapsed fold, e.g. when the
        // caret is moved into it programmatically. Click is deliberately NOT set — Scintilla
        // would then toggle the fold entirely internally and suppress MarginClick for that click
        // (confirmed live), leaving no way to know a fold changed and reposition diagnostic
        // markers. scintilla_MarginClick below does the toggle itself instead.
        scintilla.AutomaticFold = ScintillaNET.AutomaticFold.Show;
    }

    /// <summary>
    /// Configures Scintilla styling, indicators, and hover behavior.
    /// </summary>
    private void InitialiseScintilla()
    {
        // DirectWrite must be set before StyleClearAll for it to take effect.
        scintilla.Technology = ScintillaNET.Technology.DirectWrite;

        scintilla.Styles[ScintillaNET.Style.Default].Font = "Cascadia Code";
        scintilla.Styles[ScintillaNET.Style.Default].SizeF = 9.5f;

        // Line spacing — adds a little breathing room without changing the font size.
        scintilla.ExtraAscent = 1;
        scintilla.ExtraDescent = 1;

        // Scroll width follows the longest line automatically.
        scintilla.ScrollWidthTracking = true;

        // Tab and indent settings — 4 spaces, no tab characters.
        scintilla.TabWidth = 4;
        scintilla.IndentWidth = 4;
        scintilla.UseTabs = false;
        scintilla.TabIndents = true;
        scintilla.BackspaceUnindents = true;

        scintilla.MouseDwellTime = 500;

        scintilla.AutoCIgnoreCase = true;
        scintilla.AutoCOrder = ScintillaNET.Order.Custom;
        scintilla.AutoCMaxHeight = 12;
        scintilla.AutoCDropRestOfWord = true;

        // Fill-up characters: typing one of these while the list is open accepts the
        // highlighted entry and then inserts the character itself, matching Visual Studio's
        // ".", "(" and "[" commit behavior.
        scintilla.AutoCSetFillUps(".([");

        scintilla.Indicators[ScintillaErrorIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Squiggle;
        scintilla.Indicators[ScintillaWarningIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Squiggle;
        scintilla.Indicators[ScintillaHighlightIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Box;

        ApplyTheme();
    }

    /// <summary>
    /// Applies <see cref="Theme"/> to every themed Scintilla element: the default text style,
    /// caret line, selection, per-classification syntax styles, brace highlighting, diagnostic
    /// indicators, the fold margin, and the autocomplete list's selected-item background. Safe to
    /// call repeatedly — e.g. when <see cref="Theme"/> is reassigned after construction.
    /// </summary>
    private void ApplyTheme()
    {
        // Caret line highlight. Setting this colour is what makes the caret line render —
        // Scintilla 5 turns the element on as a side effect of colouring it, which is why
        // CaretLineVisible is obsolete. The alpha is explicit and opaque to match the
        // previous behaviour; translucency would also need CaretLineLayer off Layer.Base.
        scintilla.CaretLineBackColor = _theme.CaretLineBackground;

        scintilla.SelectionTextColor = _theme.SelectionForeground;
        scintilla.SelectionBackColor = _theme.SelectionBackground;

        // StyleClearAll copies Style.Default's colours to every style as a baseline, so it must
        // run after Default is coloured and before per-classification overrides are applied.
        scintilla.Styles[ScintillaNET.Style.Default].ForeColor = _theme.Foreground;
        scintilla.Styles[ScintillaNET.Style.Default].BackColor = _theme.Background;
        scintilla.StyleClearAll();

        foreach (var entry in _classificationKindToScintillaStyle)
        {
            var classificationName = entry.Key;
            var styleIndex = entry.Value;
            var colorScheme = _theme.GetClassificationColorScheme(classificationName);
            scintilla.Styles[styleIndex].ForeColor = colorScheme.Foreground;

            // Transparent means "no background override" — falling back to the theme background
            // keeps classified text opaque instead of resolving to opaque white, which would show
            // as a mismatched box behind every token on a dark theme.
            scintilla.Styles[styleIndex].BackColor = colorScheme.Background == Color.Transparent
                ? _theme.Background
                : colorScheme.Background;
            scintilla.Styles[styleIndex].Bold = colorScheme.Bold;
            scintilla.Styles[styleIndex].Italic = colorScheme.Italics;
        }

        // Brace highlight styles: matching pair and unmatched brace.
        scintilla.Styles[ScintillaNET.Style.BraceLight].ForeColor = _theme.BraceMatchForeground;
        scintilla.Styles[ScintillaNET.Style.BraceLight].Bold = true;
        scintilla.Styles[ScintillaNET.Style.BraceBad].ForeColor = _theme.BraceBadForeground;
        scintilla.Styles[ScintillaNET.Style.BraceBad].Bold = true;

        scintilla.Indicators[ScintillaErrorIndicatorIndex].ForeColor = _theme.ErrorIndicatorForeColor;
        scintilla.Indicators[ScintillaWarningIndicatorIndex].ForeColor = _theme.WarningIndicatorForeColor;

        scintilla.AutocompleteListSelectedBackColor = _theme.AutocompleteSelectedBackground;

        ConfigureFoldMarkers();
        ConfigureDiagnosticMarkers();
    }

    /// <summary>
    /// Colors the error/warning gutter marker glyphs from <see cref="Theme"/>. Recoloring an
    /// existing marker number restyles every line it is already placed on, so this does not need
    /// to touch which lines currently carry a marker.
    /// </summary>
    private void ConfigureDiagnosticMarkers()
    {
        scintilla.Markers[ScintillaErrorMarkerIndex].Symbol = ScintillaNET.MarkerSymbol.Circle;
        scintilla.Markers[ScintillaErrorMarkerIndex].SetForeColor(_theme.ErrorIndicatorForeColor);
        scintilla.Markers[ScintillaErrorMarkerIndex].SetBackColor(_theme.ErrorIndicatorForeColor);

        scintilla.Markers[ScintillaWarningMarkerIndex].Symbol = ScintillaNET.MarkerSymbol.Circle;
        scintilla.Markers[ScintillaWarningMarkerIndex].SetForeColor(_theme.WarningIndicatorForeColor);
        scintilla.Markers[ScintillaWarningMarkerIndex].SetBackColor(_theme.WarningIndicatorForeColor);
    }

    /// <summary>
    /// Configures the fold margin's own background and its marker glyphs (connected plus/minus
    /// boxes), coloring both from <see cref="Theme"/>. No lexer is attached —
    /// <see cref="ScintillaNET.Scintilla.LexerName"/> is left <see langword="null"/>, same as
    /// classification — so these glyphs draw whatever fold levels <see cref="ApplyFoldingToEditor"/>
    /// sets; Scintilla never derives them on its own.
    /// </summary>
    private void ConfigureFoldMarkers()
    {
        var markerForeColor = _theme.FoldMarginForeground;
        var markerBackColor = _theme.FoldMarginBackground;

        // The fold margin's own background is separate from every marker glyph's back color above —
        // Scintilla renders it from a dedicated "fold margin colour"/"highlight colour" pair rather
        // than from Style.Default, so without this it stays whatever Scintilla's built-in default is
        // regardless of theme.
        scintilla.SetFoldMarginColor(true, markerBackColor);
        scintilla.SetFoldMarginHighlightColor(true, markerBackColor);

        int[] foldMarkerIndexes =
        [
            ScintillaNET.Marker.FolderEnd,
            ScintillaNET.Marker.FolderOpenMid,
            ScintillaNET.Marker.FolderMidTail,
            ScintillaNET.Marker.FolderTail,
            ScintillaNET.Marker.FolderSub,
            ScintillaNET.Marker.Folder,
            ScintillaNET.Marker.FolderOpen,
        ];

        foreach (var markerIndex in foldMarkerIndexes)
        {
            scintilla.Markers[markerIndex].SetForeColor(markerForeColor);
            scintilla.Markers[markerIndex].SetBackColor(markerBackColor);
        }

        scintilla.Markers[ScintillaNET.Marker.Folder].Symbol = ScintillaNET.MarkerSymbol.BoxPlus;
        scintilla.Markers[ScintillaNET.Marker.FolderOpen].Symbol = ScintillaNET.MarkerSymbol.BoxMinus;
        scintilla.Markers[ScintillaNET.Marker.FolderEnd].Symbol = ScintillaNET.MarkerSymbol.BoxPlusConnected;
        scintilla.Markers[ScintillaNET.Marker.FolderMidTail].Symbol = ScintillaNET.MarkerSymbol.TCorner;
        scintilla.Markers[ScintillaNET.Marker.FolderOpenMid].Symbol = ScintillaNET.MarkerSymbol.BoxMinusConnected;
        scintilla.Markers[ScintillaNET.Marker.FolderSub].Symbol = ScintillaNET.MarkerSymbol.VLine;
        scintilla.Markers[ScintillaNET.Marker.FolderTail].Symbol = ScintillaNET.MarkerSymbol.LCorner;
    }

    // ── Internal analysis cycle ───────────────────────────────────────────────

    /// <summary>
    /// Resets cached diagnostics, compilation state, and transient editor UI.
    /// </summary>
    private void ResetAnalysisState()
    {
        _currentDiagnostics = [];
        _currentCompiledScript = null;
        _analysedDocumentVersion = -1;
        _colouredDocumentVersion = -1;

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
        if (_suppressTextChangeHandling || !CanAccessEditor)
        {
            return;
        }

        _documentVersion++;
        ResetAnalysisState();

        // Two cadences. The fast timer colours what was just typed; the slow one produces
        // diagnostics and semantically-refined colouring once typing pauses.
        timerSyntacticColour.Stop();
        timerSyntacticColour.Start();

        timerChangeMonitor.Stop();
        timerChangeMonitor.Start();
    }

    /// <summary>
    /// Colours newly typed code from the syntax tree alone, without waiting for the full
    /// analysis pass.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void timerSyntacticColour_Tick(object sender, EventArgs e)
    {
        timerSyntacticColour.Stop();

        if (_colouredDocumentVersion != _documentVersion)
        {
            await PerformSyntacticPassAsync();
        }
    }

    /// <summary>
    /// Runs the cheap syntax-only classification pass and applies the result.
    /// </summary>
    /// <remarks>
    /// Deliberately does not touch diagnostics: squiggles stay the business of the full pass,
    /// which alone can tell whether code is actually wrong. This only repaints colour.
    /// </remarks>
    private async Task PerformSyntacticPassAsync()
    {
        // Skip while the full pass is running: it is about to produce strictly better colouring,
        // and letting both mutate the manager's context concurrently is asking for trouble. The
        // finally block re-queues if the document has moved on by then.
        if (_manager is null ||
            _syntacticPassInProgress ||
            _analysisInProgress ||
            !TryGetScript(out var scriptSnapshot))
        {
            return;
        }

        var manager = _manager;
        var stateVersion = _editorStateVersion;
        var documentVersion = _documentVersion;

        CancelAndDispose(ref _syntacticCts);
        _syntacticCts = new CancellationTokenSource();
        var ct = _syntacticCts.Token;

        _syntacticPassInProgress = true;

        try
        {
            var syntacticResult = await manager.ApplySyntacticPassAsync(scriptSnapshot, ct);

            if (documentVersion != _documentVersion ||
                stateVersion != _editorStateVersion ||
                !CanAccessEditor)
            {
                return;
            }

            // The full pass may already have coloured this same version more precisely; do not
            // repaint over semantic colouring with the coarser syntactic result. Folding is
            // syntax-only regardless of which pass computed it, so it is always applied — and
            // since it can change which lines are hidden, diagnostic markers are repositioned
            // from the last-known diagnostics even though this pass does not recompute them.
            ApplyFoldingToEditor(syntacticResult.FoldSpans);
            ApplyDiagnosticMarkersToEditor(_currentDiagnostics);

            if (_analysedDocumentVersion == documentVersion)
            {
                return;
            }

            ApplyClassificationsToEditor(syntacticResult.Classifications);
            _colouredDocumentVersion = documentVersion;
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
        }
        finally
        {
            _syntacticPassInProgress = false;

            if (CanAccessEditor &&
                _colouredDocumentVersion != _documentVersion &&
                !timerSyntacticColour.Enabled)
            {
                timerSyntacticColour.Start();
            }
        }
    }

    /// <summary>
    /// Starts a fresh live-analysis pass once the debounce timer elapses.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void timerChangeMonitor_Tick(object sender, EventArgs e)
    {
        timerChangeMonitor.Stop();

        if (_analysedDocumentVersion != _documentVersion)
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

        // The version this pass is about to analyse. Its result is applied only if the document
        // still stands at this version when the pass returns — one rule, replacing the previous
        // whole-document string comparison.
        var documentVersion = _documentVersion;

        CancelAndDispose(ref _analysisCts);
        _analysisCts = new CancellationTokenSource();
        var ct = _analysisCts.Token;

        _analysisInProgress = true;

        try
        {
            ClearWarningAndErrorIndicators();

            // Runs on the thread pool inside EditorManager; the await returns here on the UI
            // thread, so everything below is safe to touch Scintilla with.
            await manager.ApplyScript(scriptSnapshot, ct);

            if (documentVersion != _documentVersion ||
                stateVersion != _editorStateVersion ||
                !CanAccessEditor)
            {
                return;
            }

            _currentDiagnostics = manager.LastDiagnostics;
            _analysedDocumentVersion = documentVersion;

            // Semantic colouring supersedes whatever the syntactic pass painted, so this
            // version counts as coloured too and the fast timer has nothing left to do.
            _colouredDocumentVersion = documentVersion;

            ApplyDiagnosticsToEditor(_currentDiagnostics);
            ApplyClassificationsToEditor(manager.LastClassifications);
            ApplyFoldingToEditor(manager.LastFoldSpans);
            ApplyDiagnosticMarkersToEditor(_currentDiagnostics);

            DiagnosticsUpdated?.Invoke(this, new Editors.DiagnosticsUpdatedEventArgs(_currentDiagnostics));
            ScriptChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
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

            // A superseded pass leaves the document unanalysed, so queue another one.
            if (CanAccessEditor &&
                _analysedDocumentVersion != _documentVersion &&
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
    /// Recomputes Scintilla's per-line fold levels from the given foldable ranges.
    /// </summary>
    /// <remarks>
    /// Mirrors the brace-depth algorithm classic C-like lexers use to derive fold levels: a
    /// line's own level is its nesting depth <i>before</i> any block it opens on that line, and
    /// it is marked a fold header exactly when that depth increases by the end of the line. Here
    /// the depth changes come from Roslyn's <paramref name="foldSpans"/> rather than a raw
    /// character scan, so braces inside strings, chars, and comments are never mistaken for
    /// structure.
    /// </remarks>
    /// <param name="foldSpans">The foldable ranges for the current document, as character spans.</param>
    private void ApplyFoldingToEditor(IReadOnlyList<Folding.FoldSpan> foldSpans)
    {
        if (!CanAccessEditor)
        {
            return;
        }

        var lineCount = scintilla.Lines.Count;
        var opens = new int[lineCount];
        var closes = new int[lineCount];

        foreach (var span in foldSpans)
        {
            var startLine = scintilla.LineFromPosition(span.SpanStart);
            var endLine = scintilla.LineFromPosition(span.SpanStart + span.SpanLength - 1);

            // A span confined to one line has nothing to hide when folded, so it is not a fold
            // point at all.
            if (endLine <= startLine)
            {
                continue;
            }

            opens[startLine]++;
            closes[endLine]++;
        }

        var depth = 0;

        for (var line = 0; line < lineCount; line++)
        {
            var levelBeforeLine = depth;
            depth = Math.Max(0, depth + opens[line] - closes[line]);

            var scintillaLine = scintilla.Lines[line];
            scintillaLine.FoldLevel = ScintillaFoldLevelBase + Math.Min(levelBeforeLine, ScintillaMaxFoldDepth);
            scintillaLine.FoldLevelFlags = levelBeforeLine < depth
                ? ScintillaNET.FoldLevelFlags.Header
                : ScintillaNET.FoldLevelFlags.None;
        }
    }

    /// <summary>
    /// Places error/warning gutter markers for the given diagnostics. A diagnostic hidden inside
    /// a collapsed fold is aggregated onto its nearest visible ancestor header line instead, so it
    /// stays visible while the region containing it is collapsed; where more than one diagnostic
    /// lands on the same line, the worse severity wins.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to render as gutter markers.</param>
    private void ApplyDiagnosticMarkersToEditor(ImmutableArray<Diagnostic> diagnostics)
    {
        if (!CanAccessEditor)
        {
            return;
        }

        scintilla.MarkerDeleteAll(ScintillaErrorMarkerIndex);
        scintilla.MarkerDeleteAll(ScintillaWarningMarkerIndex);

        Dictionary<int, DiagnosticSeverity>? worstSeverityByLine = null;

        foreach (var diagnostic in diagnostics)
        {
            if (!diagnostic.Location.IsInSource ||
                diagnostic.Severity is not (DiagnosticSeverity.Error or DiagnosticSeverity.Warning))
            {
                continue;
            }

            var line = GetNearestVisibleLine(scintilla.LineFromPosition(diagnostic.Location.SourceSpan.Start));

            if (line < 0)
            {
                continue;
            }

            worstSeverityByLine ??= [];

            if (!worstSeverityByLine.TryGetValue(line, out var existingSeverity) || diagnostic.Severity > existingSeverity)
            {
                worstSeverityByLine[line] = diagnostic.Severity;
            }
        }

        if (worstSeverityByLine is null)
        {
            return;
        }

        foreach (var entry in worstSeverityByLine)
        {
            var markerIndex = entry.Value == DiagnosticSeverity.Error
                ? ScintillaErrorMarkerIndex
                : ScintillaWarningMarkerIndex;

            scintilla.Lines[entry.Key].MarkerAdd(markerIndex);
        }
    }

    /// <summary>
    /// Walks up through collapsed ancestor folds from <paramref name="line"/> until reaching a
    /// line that is actually visible, so a marker placed there is never silently hidden by
    /// Scintilla's own fold-aware rendering.
    /// </summary>
    /// <param name="line">The 0-based line to start from.</param>
    /// <returns>The nearest visible line at or above <paramref name="line"/>, or -1 if none.</returns>
    private int GetNearestVisibleLine(int line)
    {
        while (line >= 0 && !scintilla.Lines[line].Visible)
        {
            line = scintilla.Lines[line].FoldParent;
        }

        return line;
    }

    /// <summary>
    /// Expands every folded region in the editor.
    /// </summary>
    private void ExpandAllFolds()
    {
        if (!CanAccessEditor)
        {
            return;
        }

        scintilla.FoldAll(ScintillaNET.FoldAction.Expand);
        ApplyDiagnosticMarkersToEditor(_currentDiagnostics);
    }

    /// <summary>
    /// Collapses every foldable region in the editor, including regions nested inside an
    /// already-collapsed one.
    /// </summary>
    private void CollapseAllFolds()
    {
        if (!CanAccessEditor)
        {
            return;
        }

        // ContractEveryLevel (rather than plain Contract) also collapses folds nested inside a
        // fold that is itself collapsing, so re-expanding the outer one later does not reveal an
        // inner region left expanded from before.
        scintilla.FoldAll(ScintillaNET.FoldAction.ContractEveryLevel);
        ApplyDiagnosticMarkersToEditor(_currentDiagnostics);
    }

    /// <summary>
    /// Returns the 0-based line number of every fold header currently collapsed in the editor.
    /// </summary>
    private IReadOnlyList<int> GetCollapsedFoldLines()
    {
        if (!CanAccessEditor)
        {
            return [];
        }

        var collapsedLines = new List<int>();

        for (var line = 0; line < scintilla.Lines.Count; line++)
        {
            var scintillaLine = scintilla.Lines[line];

            if (scintillaLine.FoldLevelFlags.HasFlag(ScintillaNET.FoldLevelFlags.Header) && !scintillaLine.Expanded)
            {
                collapsedLines.Add(line);
            }
        }

        return collapsedLines;
    }

    /// <summary>
    /// Restores a previously captured set of collapsed fold lines (see <see cref="GetCollapsedFoldLines"/>).
    /// </summary>
    /// <remarks>
    /// Everything is expanded first, so this is idempotent regardless of the editor's current fold
    /// state. A line that is no longer a fold header — the script has changed since the lines were
    /// captured — is silently skipped rather than treated as an error, the same tolerance
    /// <see cref="Folding.FoldSpanCalculator"/> gives an unmatched brace or directive.
    /// </remarks>
    /// <param name="lines">The 0-based line numbers to collapse.</param>
    private void SetCollapsedFoldLines(IReadOnlyList<int> lines)
    {
        if (!CanAccessEditor)
        {
            return;
        }

        scintilla.FoldAll(ScintillaNET.FoldAction.Expand);

        foreach (var lineNumber in lines)
        {
            if (lineNumber < 0 || lineNumber >= scintilla.Lines.Count)
            {
                continue;
            }

            var scintillaLine = scintilla.Lines[lineNumber];

            if (scintillaLine.FoldLevelFlags.HasFlag(ScintillaNET.FoldLevelFlags.Header))
            {
                scintillaLine.FoldLine(ScintillaNET.FoldAction.Contract);
            }
        }

        ApplyDiagnosticMarkersToEditor(_currentDiagnostics);
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
    private void HighlightText(int start, int length)
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
        boundedStart = Math.Min(Math.Max(start, 0), documentLength);
        boundedLength = Math.Min(length, documentLength - boundedStart);

        return boundedLength > 0;
    }

    /// <summary>
    /// Clears any active highlight range from the editor.
    /// </summary>
    private void ClearHighlightText()
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
        // Text-change notification comes from Insert/Delete, not from here: CharAdded is a
        // typing event, so it only reports characters the user typed. This handler is left
        // with the behaviours that genuinely are typing-specific — auto-indent and the
        // completion/call-tip triggers.
        var ch = (char)e.Char;

        if (ch == '\n')
        {
            var currentLine = scintilla.CurrentLine;

            if (currentLine > 0)
            {
                scintilla.Lines[currentLine].Indentation = scintilla.Lines[currentLine - 1].Indentation;
                scintilla.GotoPosition(scintilla.Lines[currentLine].IndentPosition);
            }
        }
        else if (ch == '.')
        {
            // Member access — cancel any open session and immediately start a fresh one.
            scintilla.AutoCCancel();
            StartCompletionSession(immediate: true);
        }
        else if (ch == '(')
        {
            scintilla.AutoCCancel();
            CancelCompletion();
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
            CancelCompletion();
        }
    }

    /// <summary>
    /// Handles text insertion events from Scintilla.
    /// </summary>
    /// <remarks>
    /// Paired with <see cref="scintilla_Delete"/>, this is the single point at which the editor
    /// learns that the document changed. Scintilla raises these for every modification whatever
    /// its origin — typing, paste, undo, redo, autocomplete insertion, or a programmatic edit —
    /// so no route into the document can leave the analysis stale.
    /// </remarks>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_Insert(object sender, ScintillaNET.ModificationEventArgs e) =>
        HandleTextChanged();

    /// <summary>
    /// Handles text deletion events from Scintilla.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_Delete(object sender, ScintillaNET.ModificationEventArgs e) =>
        HandleTextChanged();

    /// <summary>
    /// Highlights matching brace pairs when the caret is adjacent to a brace character.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_UpdateUI(object sender, ScintillaNET.UpdateUIEventArgs e)
    {
        var pos = scintilla.CurrentPosition;

        // Check the character at and just before the caret for a brace.
        var bracePos = ScintillaNET.Scintilla.InvalidPosition;

        if (pos > 0 && IsBrace(scintilla.GetCharAt(pos - 1)))
        {
            bracePos = pos - 1;
        }
        else if (IsBrace(scintilla.GetCharAt(pos)))
        {
            bracePos = pos;
        }

        if (bracePos == ScintillaNET.Scintilla.InvalidPosition)
        {
            scintilla.BraceHighlight(ScintillaNET.Scintilla.InvalidPosition, ScintillaNET.Scintilla.InvalidPosition);
            return;
        }

        var matchPos = scintilla.BraceMatch(bracePos);

        if (matchPos == ScintillaNET.Scintilla.InvalidPosition)
        {
            scintilla.BraceBadLight(bracePos);
        }
        else
        {
            scintilla.BraceHighlight(bracePos, matchPos);
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when the character is a recognised brace glyph.
    /// </summary>
    private static bool IsBrace(int c) => c is '(' or ')' or '{' or '}' or '[' or ']';

    /// <summary>
    /// Repositions diagnostic gutter markers after any UI update that might change which lines
    /// are visible — most importantly the user manually expanding or collapsing a fold.
    /// Scintilla's <see cref="ScintillaNET.AutomaticFold.Click"/> handles that toggle entirely
    /// internally and raises no dedicated fold-changed notification, so <c>UpdateUI</c> (which
    /// Scintilla does reliably raise afterwards) is the hook used instead. It also fires on far
    /// more than just fold toggles — every caret move and scroll — but re-placing markers from
    /// the already-known diagnostics is cheap enough that filtering those out isn't worthwhile.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_UpdateUI_DiagnosticMarkers(object sender, ScintillaNET.UpdateUIEventArgs e) =>
        ApplyDiagnosticMarkersToEditor(_currentDiagnostics);

    /// <summary>
    /// Toggles the fold under a fold-margin click. <see cref="ScintillaNET.AutomaticFold.Click"/>
    /// is deliberately not used (see <see cref="InitialiseScintilla"/>) specifically so this
    /// handler runs and can reposition diagnostic markers after the toggle.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_MarginClick(object sender, ScintillaNET.MarginClickEventArgs e)
    {
        if (e.Margin != ScintillaFoldMarginIndex)
        {
            return;
        }

        var line = scintilla.Lines[scintilla.LineFromPosition(e.Position)];

        if (line.FoldLevelFlags.HasFlag(ScintillaNET.FoldLevelFlags.Header))
        {
            line.ToggleFold();
            ApplyDiagnosticMarkersToEditor(_currentDiagnostics);
        }
    }

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
                apiInfo = await manager.GetAPIInfo(position, ct);
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
    /// <param name="immediate">
    /// When <see langword="false"/> the request is debounced so rapid typing fires one Roslyn
    /// request per word rather than one per character.
    /// </param>
    /// <remarks>
    /// The debounce is a timer, not a cancelled <see cref="Task.Delay(int, CancellationToken)"/>.
    /// Cancelling a delay throws, and with a request started on every letter that meant a
    /// <see cref="TaskCanceledException"/> plus an <see cref="OperationCanceledException"/> per
    /// keystroke — measured at ~2 per character, enough to bury a host application's own output.
    /// Restarting a timer means a superseded request simply never begins.
    /// </remarks>
    private void StartCompletionSession(bool immediate)
    {
        timerCompletion.Stop();

        if (!immediate)
        {
            timerCompletion.Start();
            return;
        }

        BeginCompletionRequest();
    }

    /// <summary>
    /// Fires the debounced completion request once typing has paused.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void timerCompletion_Tick(object sender, EventArgs e)
    {
        timerCompletion.Stop();
        BeginCompletionRequest();
    }

    /// <summary>
    /// Cancels any request still in flight and issues a fresh one.
    /// </summary>
    private void BeginCompletionRequest()
    {
        CancelAndDispose(ref _completionCts);
        _completionCts = new CancellationTokenSource();
        _ = ShowCompletionAsync(_completionCts.Token);
    }

    /// <summary>
    /// Stops a debounced completion request that has not started yet, and cancels one that has.
    /// </summary>
    private void CancelCompletion()
    {
        timerCompletion.Stop();
        _completionCts?.Cancel();
    }

    /// <summary>
    /// Fetches completions from Roslyn and populates the Scintilla autocomplete list.
    /// </summary>
    /// <remarks>
    /// Debouncing happens before this is called — see <see cref="StartCompletionSession"/>. The
    /// Roslyn work runs on the thread pool inside <see cref="Editors.EditorManager"/>; each await
    /// returns here on the UI thread, so everything touching Scintilla is safe.
    /// </remarks>
    private async Task ShowCompletionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var manager = _manager;
            var stateVersion = _editorStateVersion;

            if (cancellationToken.IsCancellationRequested ||
                manager is null ||
                !TryGetScript(out var script))
                return;

            // Keep the Roslyn document current without paying for a full diagnostics pass.
            await manager.UpdateScriptDocumentAsync(script, cancellationToken);

            if (cancellationToken.IsCancellationRequested ||
                stateVersion != _editorStateVersion ||
                !ReferenceEquals(manager, _manager) ||
                !TryGetCurrentPosition(out var currentPosition))
                return;

            int wordStart = scintilla.WordStartPosition(currentPosition, onlyWordCharacters: true);
            int lenEntered = currentPosition - wordStart;

            var completions = await manager.GetAutoCompletions(currentPosition, cancellationToken);

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
        if (_commentChordStartedAt is DateTime chordStartedAt)
        {
            _commentChordStartedAt = null;

            if (DateTime.UtcNow - chordStartedAt <= CommentChordTimeout)
            {
                if (e.KeyCode == Keys.C && !e.Control && !e.Alt)
                {
                    CommentSelectedLines();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.KeyCode == Keys.U && !e.Control && !e.Alt)
                {
                    UncommentSelectedLines();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
            }
        }

        if (e.KeyCode == Keys.E && e.Control && !e.Shift && !e.Alt)
        {
            _commentChordStartedAt = DateTime.UtcNow;
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Space && e.Control && e.Shift)
        {
            _ = StartCallTipSessionAsync();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.F && e.Control && !e.Shift && !e.Alt)
        {
            EnsureFindReplaceForm().OpenFind();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.H && e.Control && !e.Shift && !e.Alt)
        {
            EnsureFindReplaceForm().OpenReplace();
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
            CancelCompletion();
            CancelAndDispose(ref _callTipCts);
            _callTipSession?.Cancel();
            _callTipSession = null;
            _apiInfoForm.Hide();
        }
        else if (_callTipSession is not null && !e.Control && !e.Alt)
        {
            if (!scintilla.CallTipActive)
            {
                // The call tip was dismissed externally (focus change, mouse click, etc.)
                // without going through our Cancel path. Drop the stale session so that
                // Up/Down pass through to Scintilla for normal cursor movement.
                _callTipSession = null;
            }
            else if (e.KeyCode == Keys.Up)
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

    // ── Line commenting (Ctrl+E, C / Ctrl+E, U — matches Visual Studio) ──────

    /// <summary>
    /// Line-comments every line touched by the current selection, or the caret line when there is
    /// no selection.
    /// </summary>
    private void CommentSelectedLines()
    {
        if (!CanAccessEditor)
        {
            return;
        }

        var (startLine, endLine) = GetSelectedLineRange();

        scintilla.BeginUndoAction();
        _suppressTextChangeHandling = true;

        try
        {
            for (int line = startLine; line <= endLine; line++)
            {
                scintilla.InsertText(scintilla.Lines[line].Position, "//");
            }
        }
        finally
        {
            _suppressTextChangeHandling = false;
            scintilla.EndUndoAction();
        }

        SelectLines(startLine, endLine);

        // One notification for the whole block: each InsertText above raises its own Insert
        // event, which would otherwise restart the debounce timer once per selected line.
        HandleTextChanged();
    }

    /// <summary>
    /// Removes a leading "//" line-comment marker from every line touched by the current
    /// selection, or the caret line when there is no selection.
    /// </summary>
    private void UncommentSelectedLines()
    {
        if (!CanAccessEditor)
        {
            return;
        }

        var (startLine, endLine) = GetSelectedLineRange();

        scintilla.BeginUndoAction();
        _suppressTextChangeHandling = true;

        try
        {
            for (int line = startLine; line <= endLine; line++)
            {
                var scintillaLine = scintilla.Lines[line];
                var text = scintillaLine.Text;
                var trimmed = text.TrimStart(' ', '\t');

                if (!trimmed.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                var commentOffset = text.Length - trimmed.Length;
                scintilla.DeleteRange(scintillaLine.Position + commentOffset, 2);
            }
        }
        finally
        {
            _suppressTextChangeHandling = false;
            scintilla.EndUndoAction();
        }

        SelectLines(startLine, endLine);

        // One notification for the whole block, as in CommentSelectedLines.
        HandleTextChanged();
    }

    /// <summary>
    /// Returns the zero-based first and last line touched by the current selection. A selection
    /// that ends exactly at the start of a line excludes that trailing line, matching Visual
    /// Studio's Comment/Uncomment Selection commands.
    /// </summary>
    private (int startLine, int endLine) GetSelectedLineRange()
    {
        int startLine = scintilla.LineFromPosition(scintilla.SelectionStart);
        int endLine = scintilla.LineFromPosition(scintilla.SelectionEnd);

        if (endLine > startLine && scintilla.Lines[endLine].Position == scintilla.SelectionEnd)
        {
            endLine--;
        }

        return (startLine, endLine);
    }

    /// <summary>
    /// Selects the full text of the given line range, reflecting the document as it stands after
    /// a comment/uncomment edit.
    /// </summary>
    private void SelectLines(int startLine, int endLine)
    {
        var start = scintilla.Lines[startLine].Position;
        var endLineText = scintilla.Lines[endLine].Text.TrimEnd('\r', '\n');
        var end = scintilla.Lines[endLine].Position + endLineText.Length;

        scintilla.SetSelection(end, start);
    }

    /// <summary>
    /// Shows the autocomplete list at the current caret position (explicit Ctrl+Space trigger).
    /// </summary>
    private void TryRunAutoComplete()
    {
        scintilla.AutoCCancel();
        StartCompletionSession(immediate: true);
    }

    /// <summary>
    /// Returns the shared Find / Replace form, creating it on first use.
    /// </summary>
    private FormFindReplace EnsureFindReplaceForm()
    {
        if (_findReplaceForm is null || _findReplaceForm.IsDisposed)
        {
            _findReplaceForm = new FormFindReplace(scintilla);
        }

        return _findReplaceForm;
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

        // Each session supersedes the last, so abandon the Roslyn work behind the old one
        // rather than letting it run to completion and discarding the answer.
        CancelAndDispose(ref _callTipCts);
        _callTipCts = new CancellationTokenSource();
        var ct = _callTipCts.Token;

        var manager = _manager;
        var stateVersion = _editorStateVersion;

        if (!TryGetScript(out var script))
        {
            return;
        }

        try
        {
            await manager.UpdateScriptDocumentAsync(script, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
            return;
        }

        if (ct.IsCancellationRequested ||
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !TryGetCurrentPosition(out var pos))
        {
            return;
        }

        APIInfo.CallTipContext? context;

        try
        {
            context = await manager.GetCallTipContext(pos, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
            return;
        }

        if (context is null ||
            ct.IsCancellationRequested ||
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
            return;

        // API info is resolved at the character just before '(' to land on the method name.
        APIInfo.APIInfoResult? apiInfo;

        try
        {
            apiInfo = await manager.GetAPIInfo(Math.Max(0, context.OpenParenPosition - 1), ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
            return;
        }

        if (ct.IsCancellationRequested ||
            apiInfo?.MemberInfos is null ||
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

        // Shares the active session's token: starting a new session abandons this update too.
        var ct = _callTipCts?.Token ?? CancellationToken.None;

        if (!TryGetScript(out var script))
        {
            return;
        }

        try
        {
            await manager.UpdateScriptDocumentAsync(script, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ObjectDisposedException) when (
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !CanAccessEditor)
        {
            return;
        }

        if (ct.IsCancellationRequested ||
            stateVersion != _editorStateVersion ||
            !ReferenceEquals(manager, _manager) ||
            !ReferenceEquals(callTipSession, _callTipSession) ||
            !TryGetCurrentPosition(out var currentPosition))
        {
            return;
        }

        APIInfo.CallTipContext? context;

        try
        {
            context = await manager.GetCallTipContext(currentPosition, ct);
        }
        catch (OperationCanceledException)
        {
            return;
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
        timerCompletion.Stop();
        CancelAndDispose(ref _completionCts);
        CancelAndDispose(ref _dwellCts);
        CancelAndDispose(ref _callTipCts);
        CancelAndDispose(ref _analysisCts);
        CancelAndDispose(ref _syntacticCts);
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

    // ── Nested API facade class ───────────────────────────────────────────────

    /// <summary>
    /// Custom API surface for <see cref="ScintillaScriptEditor"/>.
    /// Access via <c>control.API.Xxx</c>.
    /// </summary>
    public sealed class ScintillaScriptEditorApi
    {
        private readonly ScintillaScriptEditor _ctrl;

        internal ScintillaScriptEditorApi(ScintillaScriptEditor ctrl) => _ctrl = ctrl;

        /// <summary>Raised when the set of Roslyn diagnostics for the current script changes.</summary>
        public event EventHandler<Editors.DiagnosticsUpdatedEventArgs>? DiagnosticsUpdated
        {
            add => _ctrl.DiagnosticsUpdated += value;
            remove => _ctrl.DiagnosticsUpdated -= value;
        }

        /// <summary>Raised when the text content of the script is modified by the user.</summary>
        public event EventHandler? ScriptChanged
        {
            add => _ctrl.ScriptChanged += value;
            remove => _ctrl.ScriptChanged -= value;
        }

        /// <summary>The underlying engine manager; <see langword="null"/> until <see cref="Environment"/> is set.</summary>
        public Editors.EditorManager? Manager => _ctrl.Manager;

        /// <summary>The scripting environment (assembly references, namespace imports, global type).</summary>
        public ScriptEnvironment? Environment
        {
            get => _ctrl.Environment;
            set => _ctrl.Environment = value;
        }

        /// <summary>Gets or sets the script text shown in the editor.</summary>
        public string Script
        {
            get => _ctrl.Script;
            set => _ctrl.Script = value;
        }

        /// <summary><see langword="true"/> when the most recent analysis found at least one error.</summary>
        public bool HasErrors => _ctrl.HasErrors;

        /// <summary>Diagnostics produced by the most recent live-analysis pass.</summary>
        public IReadOnlyList<Diagnostic> CurrentDiagnostics => _ctrl.CurrentDiagnostics;

        /// <summary>The last successfully compiled script, or <see langword="null"/> if the script has changed since the last <see cref="CompileAsync()"/> call.</summary>
        public ExecutableScript? CurrentCompiledScript => _ctrl.CurrentCompiledScript;

        /// <summary>Compiles the current script and returns the result.</summary>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="Environment"/> has not been set.</exception>
        public Task<ExecutableScript> CompileAsync() => _ctrl.CompileAsync();

        /// <summary>Compiles the current script and returns the result.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="Environment"/> has not been set.</exception>
        public Task<ExecutableScript> CompileAsync(CancellationToken cancellationToken) =>
            _ctrl.CompileAsync(cancellationToken);

        /// <summary>Highlights the specified text range in the editor.</summary>
        /// <param name="start">The zero-based start position.</param>
        /// <param name="length">The length of the range to highlight.</param>
        public void HighlightText(int start, int length) => _ctrl.HighlightText(start, length);

        /// <summary>Clears any active highlight range from the editor.</summary>
        public void ClearHighlightText() => _ctrl.ClearHighlightText();

        /// <summary>Expands every folded region in the editor.</summary>
        public void ExpandAllFolds() => _ctrl.ExpandAllFolds();

        /// <summary>Collapses every foldable region in the editor, including nested ones.</summary>
        public void CollapseAllFolds() => _ctrl.CollapseAllFolds();

        /// <summary>
        /// The 0-based line number of every fold currently collapsed in the editor. Setting this
        /// expands everything else first, so it is safe to use to restore a previously saved
        /// state regardless of what is currently folded.
        /// </summary>
        /// <remarks>
        /// The host is responsible for persisting this alongside <see cref="Script"/> — a file on
        /// disk, application settings, a database row — the same way it already owns persisting
        /// the script text itself. Lines are only meaningful relative to the script text they were
        /// captured against; a line that is no longer a fold header when restored is skipped
        /// rather than treated as an error.
        /// </remarks>
        public IReadOnlyList<int> CollapsedFoldLines
        {
            get => _ctrl.GetCollapsedFoldLines();
            set => _ctrl.SetCollapsedFoldLines(value);
        }
    }
}
