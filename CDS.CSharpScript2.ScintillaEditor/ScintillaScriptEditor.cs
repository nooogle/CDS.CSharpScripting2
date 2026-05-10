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

    private readonly ToolTipDiagnostics _diagnosticsToolTipManager;
    private readonly FormAPIInfo _apiInfoForm = new();
    private readonly Classification.Coloriser _coloriser = new();

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
        get => scintilla.Text;
        set => scintilla.Text = value;
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
        _diagnosticsToolTipManager.ClearHover();
        ClearWarningAndErrorIndicators();
        _apiInfoForm.Hide();
    }

    /// <summary>
    /// Handles text edits by clearing cached analysis and restarting the debounce timer.
    /// </summary>
    private void HandleTextChanged()
    {
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

        if (_lastScript != Script)
        {
            await PerformLiveAnalysisAsync();
        }
    }

    /// <summary>
    /// Performs a live-analysis pass and updates diagnostics, classifications, and events.
    /// </summary>
    private async Task PerformLiveAnalysisAsync()
    {
        if (_manager is null)
        {
            return;
        }

        ClearWarningAndErrorIndicators();

        await _manager.ApplyScript(Script);

        _currentDiagnostics = _manager.LastDiagnostics;
        _lastScript = Script;

        ApplyDiagnosticsToEditor(_currentDiagnostics);
        ApplyClassificationsToEditor(_manager.LastClassifications);

        DiagnosticsUpdated?.Invoke(this, new Editors.DiagnosticsUpdatedEventArgs(_currentDiagnostics));
        ScriptChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Visual feedback ───────────────────────────────────────────────────────

    /// <summary>
    /// Applies diagnostic indicators to the editor for source-based warnings and errors.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to render.</param>
    private void ApplyDiagnosticsToEditor(ImmutableArray<Diagnostic> diagnostics)
    {
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

        scintilla.IndicatorFillRange(position: start, length: length);
    }

    /// <summary>
    /// Applies syntax classification styling to the editor.
    /// </summary>
    /// <param name="classifications">The classifications to apply.</param>
    private void ApplyClassificationsToEditor(IReadOnlyList<Classification.ClassifiedSymbol> classifications)
    {
        scintilla.StartStyling(0);
        scintilla.SetStyling(scintilla.Text.Length, 0);

        foreach (var classification in classifications)
        {
            if (_classificationKindToScintillaStyle.TryGetValue(classification.Classification, out var styleIndex))
            {
                scintilla.StartStyling(classification.SpanStart);
                scintilla.SetStyling(classification.SpanLength, styleIndex);
            }
        }
    }

    /// <summary>
    /// Clears all warning and error indicators from the editor.
    /// </summary>
    private void ClearWarningAndErrorIndicators()
    {
        scintilla.IndicatorCurrent = ScintillaErrorIndicatorIndex;
        scintilla.IndicatorClearRange(0, scintilla.Text.Length);

        scintilla.IndicatorCurrent = ScintillaWarningIndicatorIndex;
        scintilla.IndicatorClearRange(0, scintilla.Text.Length);
    }

    // ── Highlight API (public — used by ClassifiedSpans and SyntaxTree demos) ─

    /// <summary>
    /// Highlights the specified text range in the editor.
    /// </summary>
    /// <param name="start">The zero-based start position.</param>
    /// <param name="length">The length of the range to highlight.</param>
    public void HighlightText(int start, int length)
    {
        ClearHighlightText();
        scintilla.IndicatorCurrent = ScintillaHighlightIndicatorIndex;
        scintilla.IndicatorFillRange(position: start, length: length);
        scintilla.ScrollCaret();
    }

    /// <summary>
    /// Clears any active highlight range from the editor.
    /// </summary>
    public void ClearHighlightText()
    {
        scintilla.IndicatorCurrent = ScintillaHighlightIndicatorIndex;
        scintilla.IndicatorClearRange(0, scintilla.Text.Length);
    }

    // ── Scintilla event handlers ──────────────────────────────────────────────

    /// <summary>
    /// Handles character insertion events from Scintilla.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_CharAdded(object sender, ScintillaNET.CharAddedEventArgs e) =>
        HandleTextChanged();

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
    /// Handles the start of a dwell operation to show hover diagnostics.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_DwellStart(object sender, ScintillaNET.DwellEventArgs e)
    {
        _diagnosticsToolTipManager.HandleDwellStart(
            diagnostics: _currentDiagnostics,
            characterPosition: e.Position);
    }

    /// <summary>
    /// Handles the end of a dwell operation to clear hover diagnostics.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_DwellEnd(object sender, ScintillaNET.DwellEventArgs e) =>
        _diagnosticsToolTipManager.HandleDwellEnd();

    /// <summary>
    /// Handles key input for editor assistance features.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void scintilla_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && e.Control)
        {
            await TryRunAutoComplete();
        }
        else if (e.KeyCode == Keys.F1)
        {
            if (_manager is null) return;

            int pos = scintilla.CurrentPosition;
            var point = new Point(
                x: scintilla.PointXFromPosition(pos),
                y: scintilla.PointYFromPosition(pos));

            var apiInfo = await _manager.GetAPIInfo(scintilla.CurrentPosition);
            _apiInfoForm.ShowAPIInfo(parent: this, location: point, apiInfo: apiInfo);
        }
        else if (e.KeyCode == Keys.Escape)
        {
            scintilla.AutoCCancel();
            _apiInfoForm.Hide();
        }
    }

    /// <summary>
    /// Shows the autocomplete list at the current caret position.
    /// </summary>
    private async Task TryRunAutoComplete()
    {
        if (_manager is null)
        {
            return;
        }

        scintilla.AutoCCancel();

        // Flush any pending change before requesting completions
        if (_lastScript != Script)
        {
            await PerformLiveAnalysisAsync();
        }

        int currentPosition = scintilla.CurrentPosition;
        int wordStartPosition = scintilla.WordStartPosition(position: currentPosition, onlyWordCharacters: true);
        int lenEntered = currentPosition - wordStartPosition;
        string word = scintilla.GetTextRange(wordStartPosition, length: lenEntered);

        var completions = await _manager.GetAutoCompletions(currentPosition);

        var completionList = string.Join(
            scintilla.AutoCSeparator.ToString(),
            completions.Select(c => c.DisplayText));

        scintilla.AutoCShow(word.Length, completionList);
    }

    /// <summary>
    /// Handles the cancellation of the autocomplete list.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_AutoCCancelled(object sender, EventArgs e) { }

    /// <summary>
    /// Handles deletion while the autocomplete list is active.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_AutoCCharDeleted(object sender, EventArgs e) { }

    /// <summary>
    /// Handles completion selection from the autocomplete list.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void scintilla_AutoCCompleted(object sender, ScintillaNET.AutoCSelectionEventArgs e) { }
}
