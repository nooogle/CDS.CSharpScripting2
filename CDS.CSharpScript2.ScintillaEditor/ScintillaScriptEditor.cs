using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.ComponentModel;

namespace CDS.CSharpScript2.ScintillaEditor;

public partial class ScintillaScriptEditor : UserControl, Editors.IScriptEditor
{
    private const string CDSCategory = "CDS";

    private const int scintillaErrorIndicatorIndex = 3;
    private const int scintillaWarningIndicatorIndex = 4;
    private const int scintillaHighlightIndicatorIndex = 5;

    private ImmutableDictionary<Classification.SymbolClassification, int> _classificationKindToScintillaStyle;
    private ImmutableArray<Diagnostic> _currentDiagnostics = [];
    private ExecutableScript? _currentCompiledScript;
    private Editors.EditorManager? _manager;
    private ScriptEnvironment? _environment;
    private string _lastScript = "";

    private ToolTipDiagnostics _diagnosticsToolTipManager;
    private FormAPIInfo _apiInfoForm = new FormAPIInfo();
    private Classification.Coloriser _coloriser = new();

    // ── IScriptEditor ────────────────────────────────────────────────────────

    [Category(CDSCategory)]
    public event EventHandler<Editors.DiagnosticsUpdatedEventArgs>? DiagnosticsUpdated;

    [Category(CDSCategory)]
    public event EventHandler? ScriptChanged;

    public Editors.EditorManager? Manager => _manager;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ScriptEnvironment? Environment
    {
        get => _environment;
        set
        {
            _environment = value;
            _manager = value is null ? null : new Editors.EditorManager(value);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Script
    {
        get => scintilla.Text;
        set => scintilla.Text = value;
    }

    public bool HasErrors =>
        _currentDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    public IReadOnlyList<Diagnostic> CurrentDiagnostics => _currentDiagnostics;

    public ExecutableScript? CurrentCompiledScript => _currentCompiledScript;

    public async Task<ExecutableScript> CompileAsync(CancellationToken cancellationToken = default)
    {
        if (_manager is null)
            throw new InvalidOperationException($"{nameof(Environment)} must be set before compiling.");

        _currentCompiledScript = await _manager.CompileAsync(cancellationToken).ConfigureAwait(false);
        return _currentCompiledScript;
    }

    // ── Construction ─────────────────────────────────────────────────────────

    public ScintillaScriptEditor()
    {
        InitializeComponent();

        _diagnosticsToolTipManager = new ToolTipDiagnostics(scintilla, toolTip);

        var builder = new Dictionary<Classification.SymbolClassification, int>();
        var names = (Classification.SymbolClassification[])Enum.GetValues(typeof(Classification.SymbolClassification));

        for (int i = 1; i <= names.Length; i++)
            builder[names[i - 1]] = i;

        _classificationKindToScintillaStyle = builder.ToImmutableDictionary();

        InitialiseScintilla();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (DesignMode) return;

        timerChangeMonitor.Start();

        scintilla.Margins[0].Type = ScintillaNET.MarginType.Number;
        scintilla.Margins[0].Width = 40;

        scintilla.Margins[1].Type = ScintillaNET.MarginType.Symbol;
        scintilla.Margins[1].Width = 8;
        scintilla.Margins[1].Sensitive = false;
        scintilla.Margins[1].Mask = 0;
    }

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

        scintilla.Indicators[scintillaErrorIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Squiggle;
        scintilla.Indicators[scintillaErrorIndicatorIndex].ForeColor = Color.Red;

        scintilla.Indicators[scintillaWarningIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Squiggle;
        scintilla.Indicators[scintillaWarningIndicatorIndex].ForeColor = Color.Green;

        scintilla.Indicators[scintillaHighlightIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Box;
    }

    // ── Internal analysis cycle ───────────────────────────────────────────────

    private void HandleTextChanged()
    {
        _currentDiagnostics = [];
        _currentCompiledScript = null;
        _diagnosticsToolTipManager.ClearHover();

        timerChangeMonitor.Stop();
        timerChangeMonitor.Start();
    }

    private void timerChangeMonitor_Tick(object sender, EventArgs e)
    {
        timerChangeMonitor.Stop();

        if (_lastScript != Script)
            PerformLiveAnalysis();
    }

    private async void PerformLiveAnalysis()
    {
        if (_manager is null) return;

        ClearWarningAndErrorIndicators();

        await _manager.ApplyScript(Script);

        // Continuation is back on the UI thread (async void captures SynchronizationContext;
        // no ConfigureAwait(false) on the outer await above).
        _currentDiagnostics = _manager.LastDiagnostics;
        _lastScript = Script;

        ApplyDiagnosticsToEditor(_currentDiagnostics);
        ApplyClassificationsToEditor(_manager.LastClassifications);

        DiagnosticsUpdated?.Invoke(this, new Editors.DiagnosticsUpdatedEventArgs(_currentDiagnostics));
        ScriptChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Visual feedback ───────────────────────────────────────────────────────

    private void ApplyDiagnosticsToEditor(ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.DefaultSeverity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
                MarkDiagnosticInEditor(diagnostic);
        }
    }

    private void MarkDiagnosticInEditor(Diagnostic diagnostic)
    {
        scintilla.IndicatorCurrent =
            diagnostic.Severity == DiagnosticSeverity.Error
            ? scintillaErrorIndicatorIndex
            : scintillaWarningIndicatorIndex;

        var start = diagnostic.Location.SourceSpan.Start;
        var length = diagnostic.Location.SourceSpan.Length;

        if (length == 0)
        {
            start = Math.Max(0, start - 1);
            length = 1;
        }

        scintilla.IndicatorFillRange(position: start, length: length);
    }

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

    private void ClearWarningAndErrorIndicators()
    {
        scintilla.IndicatorCurrent = scintillaErrorIndicatorIndex;
        scintilla.IndicatorClearRange(0, scintilla.Text.Length);

        scintilla.IndicatorCurrent = scintillaWarningIndicatorIndex;
        scintilla.IndicatorClearRange(0, scintilla.Text.Length);
    }

    // ── Highlight API (public — used by ClassifiedSpans and SyntaxTree demos) ─

    public void HighlightText(int start, int length)
    {
        ClearHighlightText();
        scintilla.IndicatorCurrent = scintillaHighlightIndicatorIndex;
        scintilla.IndicatorFillRange(position: start, length: length);
        scintilla.ScrollCaret();
    }

    public void ClearHighlightText()
    {
        scintilla.IndicatorCurrent = scintillaHighlightIndicatorIndex;
        scintilla.IndicatorClearRange(0, scintilla.Text.Length);
    }

    // ── Scintilla event handlers ──────────────────────────────────────────────

    private void scintilla_CharAdded(object sender, ScintillaNET.CharAddedEventArgs e) =>
        HandleTextChanged();

    private void scintilla_Delete(object sender, ScintillaNET.ModificationEventArgs e) =>
        HandleTextChanged();

    private void scintilla_MouseMove(object sender, MouseEventArgs e)
    {
        // Reserved for future pointer-tracking features.
    }

    private void scintilla_DwellStart(object sender, ScintillaNET.DwellEventArgs e)
    {
        _diagnosticsToolTipManager.HandleDwellStart(
            diagnostics: _currentDiagnostics,
            characterPosition: e.Position);
    }

    private void scintilla_DwellEnd(object sender, ScintillaNET.DwellEventArgs e) =>
        _diagnosticsToolTipManager.HandleDwellEnd();

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

    private async Task TryRunAutoComplete()
    {
        if (_manager is null) return;

        scintilla.AutoCCancel();

        // Flush any pending change before requesting completions
        PerformLiveAnalysis();

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

    private void scintilla_AutoCCancelled(object sender, EventArgs e) { }
    private void scintilla_AutoCCharDeleted(object sender, EventArgs e) { }
    private void scintilla_AutoCCompleted(object sender, ScintillaNET.AutoCSelectionEventArgs e) { }
}
