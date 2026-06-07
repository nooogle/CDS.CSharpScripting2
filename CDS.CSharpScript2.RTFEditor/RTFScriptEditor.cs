using System.ComponentModel;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace CDS.CSharpScript2.RTFEditor;

/// <summary>
/// Provides a RichTextBox-based script editor with live diagnostics and background classification feedback.
/// </summary>
public partial class RTFScriptEditor : UserControl, Editors.IScriptEditor
{
    private Editors.EditorManager? _manager;
    private ScriptEnvironment? _environment;
    private ImmutableArray<Diagnostic> _currentDiagnostics = [];
    private ExecutableScript? _currentCompiledScript;
    private string _lastScript = "";
    private int _programmaticTextChangeSentryDepth = 0;

    private readonly Font _errorFont;
    private readonly ToolTipManager _toolTipManager;
    private readonly Classification.Coloriser _coloriser = new();

    // ── IScriptEditor ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Editors.EditorManager? Manager => _manager;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Script
    {
        get => richTextBox.Text;
        set => richTextBox.Text = value;
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
        {
            throw new InvalidOperationException($"{nameof(Environment)} must be set before compiling.");
        }

        _currentCompiledScript = await _manager.CompileAsync(cancellationToken).ConfigureAwait(false);
        return _currentCompiledScript;
    }

    /// <inheritdoc/>
    public event EventHandler<Editors.DiagnosticsUpdatedEventArgs>? DiagnosticsUpdated;

    /// <inheritdoc/>
    public event EventHandler? ScriptChanged;

    // ── Construction ─────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="RTFScriptEditor"/> class.
    /// </summary>
    /// <remarks>
    /// Set <see cref="Editors.IScriptEditor.Environment"/> before compiling or relying on live analysis.
    /// </remarks>
    public RTFScriptEditor()
    {
        InitializeComponent();

        _errorFont = new Font(Font, newStyle: FontStyle.Underline);
        _toolTipManager = new ToolTipManager(richTextBox, toolTip);
    }

    // ── Internal analysis cycle ───────────────────────────────────────────────

    private void richTextBox_TextChanged(object sender, EventArgs e)
    {
        if (_programmaticTextChangeSentryDepth > 0)
        {
            return;
        }

        _currentDiagnostics = [];
        _currentCompiledScript = null;

        timerChangeMonitor.Stop();
        timerChangeMonitor.Start();
    }

    private void timerChangeMonitor_Tick(object sender, EventArgs e)
    {
        timerChangeMonitor.Stop();

        if (_lastScript != Script)
        {
            PerformLiveAnalysis();
        }
    }

    private async void PerformLiveAnalysis()
    {
        if (_manager is null)
        {
            return;
        }

        ClearErrorIndicators();

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
        _programmaticTextChangeSentryDepth++;

        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.DefaultSeverity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            {
                MarkDiagnosticInEditor(diagnostic);
            }
        }

        _programmaticTextChangeSentryDepth--;
    }

    private void MarkDiagnosticInEditor(Diagnostic diagnostic)
    {
        richTextBox.Select(
            start: diagnostic.Location.SourceSpan.Start,
            length: diagnostic.Location.SourceSpan.Length);
        richTextBox.SelectionFont = _errorFont;
    }

    private void ApplyClassificationsToEditor(IReadOnlyList<Classification.ClassifiedSymbol> classifications)
    {
        _programmaticTextChangeSentryDepth++;

        foreach (var classification in classifications)
        {
            var colorScheme = _coloriser.FromClassificationName(classification.Classification);

            richTextBox.Select(
                start: classification.SpanStart,
                length: classification.SpanLength);

            richTextBox.SelectionBackColor = colorScheme.Background;
        }

        _programmaticTextChangeSentryDepth--;
    }

    private void ClearErrorIndicators()
    {
        _programmaticTextChangeSentryDepth++;

        richTextBox.SelectAll();
        richTextBox.SelectionFont = Font;
        richTextBox.SelectionBackColor = Color.White;

        _programmaticTextChangeSentryDepth--;
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    private void richTextBox_MouseMove(object sender, MouseEventArgs e)
    {
        var charIndex = richTextBox.GetCharIndexFromPosition(e.Location);

        _toolTipManager.HandleMouseMove(
            diagnostics: _currentDiagnostics,
            apiInfo: null,
            characterPosition: charIndex);
    }
}
