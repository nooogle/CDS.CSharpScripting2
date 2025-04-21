using System.Collections.Immutable;

namespace CDS.CSharpScript2.ScintillaEditor;

public partial class ScintillaScriptEditor : UserControl, Editors.IEditor
{
    private ImmutableDictionary<Editors.Syntax.SimpleSyntaxKind, int> syntaxKindToScintillaStyle;
    private ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> lastDiagnostics = [];

    private const int scintillaErrorIndicatorIndex = 3;
    private const int scintillaWarningIndicatorIndex = 4;
    private string lastScript = "";

    private ToolTipDiagnostics diagnosticsToolTipManager;
    private Editors.ApplyScriptDelegateAsync processScriptAsync;
    private Editors.GetAutoCompleteListDelegateAsync getAutoCompleteListAsync;
    private Editors.GetAPIInfoDelegateAsync getAPIInfoAsync;

    private FormAPIInfo apiInfoForm = new FormAPIInfo();


    public ScintillaScriptEditor()
    {
        InitializeComponent();

        diagnosticsToolTipManager = new ToolTipDiagnostics(scintilla, toolTip);

        syntaxKindToScintillaStyle = new Dictionary<Editors.Syntax.SimpleSyntaxKind, int>
        {
            [Editors.Syntax.SimpleSyntaxKind.Keyword] = 1,
            [Editors.Syntax.SimpleSyntaxKind.Argument] = 2,
            [Editors.Syntax.SimpleSyntaxKind.Commment] = 3,
            [Editors.Syntax.SimpleSyntaxKind.XMLDocumentationComment] = 4,
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

    public void SetDelegates(
        Editors.ApplyScriptDelegateAsync processScriptAsync,
        Editors.GetAutoCompleteListDelegateAsync getAutoCompleteListAsync,
        Editors.GetAPIInfoDelegateAsync getAPIInfoAsync)
    {
        this.processScriptAsync = processScriptAsync;
        this.getAutoCompleteListAsync = getAutoCompleteListAsync;
        this.getAPIInfoAsync = getAPIInfoAsync;
    }



    private void InitialiseEditor()
    {
        //scintilla.Styles[ScintillaNET.Style.Default].Font = "Courier New";
        //scintilla.Styles[ScintillaNET.Style.Default].SizeF = 10;



        //scintilla.Lexer = ScintillaNET.Lexer.Null;
        scintilla.MouseDwellTime = 500;

        scintilla.Styles[syntaxKindToScintillaStyle[Editors.Syntax.SimpleSyntaxKind.Keyword]].ForeColor = Color.Blue;
        scintilla.Styles[syntaxKindToScintillaStyle[Editors.Syntax.SimpleSyntaxKind.Argument]].ForeColor = Color.Red;
        scintilla.Styles[syntaxKindToScintillaStyle[Editors.Syntax.SimpleSyntaxKind.Commment]].ForeColor = Color.Green;
        scintilla.Styles[syntaxKindToScintillaStyle[Editors.Syntax.SimpleSyntaxKind.XMLDocumentationComment]].ForeColor = Color.Gray;

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


    public void ApplySyntaxElements(ImmutableArray<Editors.Syntax.SyntaxElement> syntaxElements)
    {
        // clear all styling
        scintilla.StartStyling(0);
        scintilla.SetStyling(scintilla.Text.Length, 0);


        foreach (var syntaxElement in syntaxElements)
        {
            if (Editors.Syntax.SyntaxKindToSimpleGenerator.Map.TryGetValue(syntaxElement.Kind, out var simpleSyntaxKind))
            {
                scintilla.StartStyling(syntaxElement.Span.Start);
                scintilla.SetStyling(syntaxElement.Span.Length, syntaxKindToScintillaStyle[simpleSyntaxKind]);
            }
        }
    }


    private async void PerformLiveCompilationOfChangedScript()
    {
        ClearWarningAndErrorIndicators();
        await processScriptAsync?.Invoke(Script);
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

    private async void scintilla_MouseMove(object sender, MouseEventArgs e)
    {
        var characterPosition = scintilla.CharPositionFromPointClose(e.Location.X, e.Location.Y);

        //APIInfo.IAPIInfoResult apiInfo = null;
        ////characterPosition >= 0 ?
        ////await getAPIInfoAsync(characterPosition) :
        ////null;

        diagnosticsToolTipManager.HandleMouseMove(
            diagnostics: lastDiagnostics,
            characterPosition: characterPosition);
    }

    private void scintilla_AutoCCancelled(object sender, EventArgs e)
    {

    }

    private void scintilla_AutoCCharDeleted(object sender, EventArgs e)
    {

    }

    private void scintilla_AutoCCompleted(object sender, ScintillaNET.AutoCSelectionEventArgs e)
    {

    }

    private void scintilla_CharAdded(object sender, ScintillaNET.CharAddedEventArgs e)
    {
        HandleTextChanged();
    }

    private void HandleTextChanged()
    {
        lastDiagnostics = ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>.Empty;

        timerChangeMonitor.Stop();
        timerChangeMonitor.Start();
    }

    private void scintilla_Delete(object sender, ScintillaNET.ModificationEventArgs e)
    {
        HandleTextChanged();
    }


    private async void scintilla_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && e.Control)
        {
            await TryRunAutoComplete();
        }
        else if (e.KeyCode == Keys.F1)
        {
            int pos = scintilla.CurrentPosition;

            Point point = new Point(
                x: scintilla.PointXFromPosition(pos),
                y: scintilla.PointYFromPosition(pos));

            var apiInfo = await getAPIInfoAsync(scintilla.CurrentPosition);
            

            apiInfoForm.ShowAPIInfo(parent: this, location: point, apiInfo: apiInfo);
        }
        else if (e.KeyCode == Keys.Escape)
        {
            scintilla.AutoCCancel();
            apiInfoForm.Hide();
            //diagnosticsToolTipManager.ShowAPIInfo(null, position: new Point(0, 0));
        }
        else if (e.KeyCode == Keys.Right)
        {
            //diagnosticsToolTipManager.OnRightArrowKeyPressed();
        }
    }

    private async Task TryRunAutoComplete()
    {
        // Do we have a handler for this?
        if (getAutoCompleteListAsync == null) { return; }

        // Cancel any existing autocomplete
        scintilla.AutoCCancel();

        // Make sure the current script has been sent to the script processor
        PerformLiveCompilationOfChangedScript();

        // Get the word fragment at the caret position
        int currentPosition = scintilla.CurrentPosition;
        int wordStartPosition = scintilla.WordStartPosition(position: currentPosition, onlyWordCharacters: true);
        int lenEntered = currentPosition - wordStartPosition;
        string word = scintilla.GetTextRange(wordStartPosition, length: lenEntered);

        // Get the autocomplete list
        var roslynCompletionList = await getAutoCompleteListAsync(currentPosition);

        // Convert the list to a string
        var scintillaCompletionList = 
            string
            .Join(
                scintilla.AutoCSeparator.ToString(), 
                roslynCompletionList.Select(c => c.DisplayText));

        // show an autocomplete list
        scintilla.AutoCShow(word.Length, scintillaCompletionList);
    }
}
