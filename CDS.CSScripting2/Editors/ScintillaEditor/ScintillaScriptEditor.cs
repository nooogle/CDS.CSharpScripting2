using System.Collections.Immutable;

namespace CDS.CSScripting2.Editors.ScintillaEditor;

public partial class ScintillaScriptEditor : UserControl, IEditor
{
    private ImmutableDictionary<Syntax.SimpleSyntaxKind, int> syntaxKindToScintillaStyle;
    private ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> lastDiagnostics = [];

    private const int scintillaErrorIndicatorIndex = 3;
    private const int scintillaWarningIndicatorIndex = 4;
    private string lastScript = "";

    private ToolTipManager toolTipManager;
    private ProcessScriptDelegate processScript;

    public ScintillaScriptEditor()
    {
        InitializeComponent();

        toolTipManager = new ToolTipManager(scintilla, toolTip);

        syntaxKindToScintillaStyle = new Dictionary<Syntax.SimpleSyntaxKind, int>
        {
            [Syntax.SimpleSyntaxKind.Keyword] = 1,
            [Syntax.SimpleSyntaxKind.Argument] = 2,
            [Syntax.SimpleSyntaxKind.Commment] = 3,
            [Syntax.SimpleSyntaxKind.XMLDocumentationComment] = 4,
        }.ToImmutableDictionary();

        InitialiseEditor();
    }

    private void timerChangeMonitor_Tick(object sender, EventArgs e)
    {
        timerChangeMonitor.Stop();

        if (lastScript != Script)
        {
            PerformLiveCompilationOfChangedScript();
        }
    }

    public void SetProcessScriptHandler(ProcessScriptDelegate processScript)
    {
        this.processScript = processScript;
    }


    private void InitialiseEditor()
    {
        //scintilla.Styles[ScintillaNET.Style.Default].Font = "Courier New";
        //scintilla.Styles[ScintillaNET.Style.Default].SizeF = 10;

        //scintilla.Lexer = ScintillaNET.Lexer.Null;
        scintilla.MouseDwellTime = 500;

        scintilla.Styles[syntaxKindToScintillaStyle[Syntax.SimpleSyntaxKind.Keyword]].ForeColor = Color.Blue;
        scintilla.Styles[syntaxKindToScintillaStyle[Syntax.SimpleSyntaxKind.Argument]].ForeColor = Color.Red;
        scintilla.Styles[syntaxKindToScintillaStyle[Syntax.SimpleSyntaxKind.Commment]].ForeColor = Color.Green;
        scintilla.Styles[syntaxKindToScintillaStyle[Syntax.SimpleSyntaxKind.XMLDocumentationComment]].ForeColor = Color.Gray;

        scintilla.Indicators[scintillaErrorIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Squiggle;
        scintilla.Indicators[scintillaErrorIndicatorIndex].ForeColor = Color.Red;

        scintilla.Indicators[scintillaWarningIndicatorIndex].Style = ScintillaNET.IndicatorStyle.Squiggle;
        scintilla.Indicators[scintillaWarningIndicatorIndex].ForeColor = Color.Green;
    }

    public string Script
    {
        get => scintilla.Text;
        set => scintilla.Text = value;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (DesignMode) { return; }

        timerChangeMonitor.Start();
    }


    public void ApplyDiagnostics(ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics)
    {
        lastDiagnostics = diagnostics;

        foreach (var diagnostic in diagnostics)
        {
            if ((diagnostic.DefaultSeverity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error) ||
                (diagnostic.DefaultSeverity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning))
            {
                ApplyErrorOrWarningStyle(diagnostic);
            }
        }
    }


    public void ApplySyntaxElements(ImmutableArray<Syntax.SyntaxElement> syntaxElements)
    {
        foreach (var syntaxElement in syntaxElements)
        {
            if (Syntax.SyntaxKindToSimpleGenerator.Map.TryGetValue(syntaxElement.Kind, out var simpleSyntaxKind))
            {
                scintilla.StartStyling(syntaxElement.Span.Start);
                scintilla.SetStyling(syntaxElement.Span.Length, syntaxKindToScintillaStyle[simpleSyntaxKind]);
            }
        }
    }


    private void PerformLiveCompilationOfChangedScript()
    {
        ClearWarningAndErrorIndicators();
        processScript(Script);
        lastScript = Script;
    }


    private void ClearWarningAndErrorIndicators()
    {
        scintilla.IndicatorCurrent = scintillaErrorIndicatorIndex;
        scintilla.IndicatorClearRange(0, scintilla.Text.Length);

        scintilla.IndicatorCurrent = scintillaWarningIndicatorIndex;
        scintilla.IndicatorClearRange(0, scintilla.Text.Length);
    }


    private void ApplyErrorOrWarningStyle(Microsoft.CodeAnalysis.Diagnostic diagnostic)
    {
        scintilla.IndicatorCurrent =
            (diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error) ?
            scintillaErrorIndicatorIndex :
            scintillaWarningIndicatorIndex;

        var start = diagnostic.Location.SourceSpan.Start;
        var length = diagnostic.Location.SourceSpan.Length;

        if (length == 0)
        {
            start = Math.Max(0, start - 1);
            length = 1;
        }

        scintilla.IndicatorFillRange(position: start, length: length);
    }


    private void scintilla_TextChanged(object sender, EventArgs e)
    {
        lastDiagnostics = ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>.Empty;

        timerChangeMonitor.Stop();
        timerChangeMonitor.Start();
    }

    private void scintilla_MouseMove(object sender, MouseEventArgs e)
    {
        var characterPosition = scintilla.CharPositionFromPointClose(e.Location.X, e.Location.Y);

        toolTipManager.HandleMouseMove(
            diagnostics: lastDiagnostics,
            characterPosition: characterPosition);
    }
}
