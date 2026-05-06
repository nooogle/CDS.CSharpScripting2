using Microsoft.CodeAnalysis.Classification;
using System.Collections.Immutable;
using System.ComponentModel;

namespace CDS.CSharpScript2.ScintillaEditor;

public partial class ScintillaScriptEditor : UserControl, Editors.IEditor
{
    private ImmutableDictionary<Classification.SymbolClassification, int> classificationKindToScintillaStyle;
    private ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> lastDiagnostics = [];

    private const int scintillaErrorIndicatorIndex = 3;
    private const int scintillaWarningIndicatorIndex = 4;
    private const int scintillaHighlightIndicatorIndex = 5;
    private string lastScript = "";

    private ToolTipDiagnostics diagnosticsToolTipManager;
    private Editors.ApplyScriptDelegateAsync processScriptAsync;
    private Editors.GetAutoCompleteListDelegateAsync getAutoCompleteListAsync;
    private Editors.GetAPIInfoDelegateAsync getAPIInfoAsync;

    private FormAPIInfo apiInfoForm = new FormAPIInfo();


    private Classification.Coloriser _coloriser = new();

    [CDSCategory()]
    public event EventHandler OnScriptChanged;


    public ScintillaScriptEditor()
    {
        InitializeComponent();

        diagnosticsToolTipManager = new ToolTipDiagnostics(scintilla, toolTip);

        var classificationKindToScintillaStyleBuilder = new Dictionary<Classification.SymbolClassification, int>();
        var coloriserNames = (Classification.SymbolClassification[])Enum.GetValues(typeof(Classification.SymbolClassification));

        for (int styleIndex = 1; styleIndex <= coloriserNames.Length; styleIndex++)
        {
            classificationKindToScintillaStyleBuilder[coloriserNames[styleIndex - 1]] = styleIndex;
        }

        classificationKindToScintillaStyle = classificationKindToScintillaStyleBuilder.ToImmutableDictionary();

        InitialiseEditor();
    }

    private void timerChangeMonitor_Tick(object sender, EventArgs e)
    {
        timerChangeMonitor.Stop();

        if (lastScript != Script)
        {
            PerformLiveCompilationOfChangedScript();

            OnScriptChanged?.Invoke(this, EventArgs.Empty);
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
        scintilla.Styles[ScintillaNET.Style.Default].Font = "Cascadia Mono";
        scintilla.Styles[ScintillaNET.Style.Default].SizeF = 10;
        scintilla.StyleClearAll(); // This propagates the default style to all styles


        //scintilla.Lexer = ScintillaNET.Lexer.Null;
        scintilla.MouseDwellTime = 500;

        foreach (var classificationName in classificationKindToScintillaStyle.Keys)
        {
            var styleIndex = classificationKindToScintillaStyle[classificationName];
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
        //scintilla.Indicators[scintillaWarningIndicatorIndex].ForeColor = Color.Green;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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

        // Set up line numbers margin (margin 0)
        scintilla.Margins[0].Type = ScintillaNET.MarginType.Number;
        scintilla.Margins[0].Width = 40;  // Width for the line numbers

        // Create a spacing margin (margin 1) for gap between numbers and text
        scintilla.Margins[1].Type = ScintillaNET.MarginType.Symbol;
        scintilla.Margins[1].Width = 8;  // Gap width in pixels
        scintilla.Margins[1].Sensitive = false;
        scintilla.Margins[1].Mask = 0;  // No markers
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


    public void ApplyClassifications(IReadOnlyList<Classification.ClassifiedSymbol> classifications)
    {
        // clear all styling
        scintilla.StartStyling(0);
        scintilla.SetStyling(scintilla.Text.Length, 0);


        foreach (var classification in classifications)
        {
            if (classificationKindToScintillaStyle.TryGetValue(classification.Classification, out var styleIndex))
            {
                scintilla.StartStyling(classification.SpanStart);
                scintilla.SetStyling(classification.SpanLength, styleIndex);
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


    public void HighlightText(int start, int length)
    {
        ClearHighlightText();

        scintilla.IndicatorCurrent = scintillaHighlightIndicatorIndex;
        scintilla.IndicatorFillRange(position: start, length: length);
        scintilla.ScrollCaret();
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

    public void ClearHighlightText()
    {
        scintilla.IndicatorCurrent = scintillaHighlightIndicatorIndex;
        scintilla.IndicatorClearRange(0, scintilla.Text.Length);
    }

    private void scintilla_DwellStart(object sender, ScintillaNET.DwellEventArgs e)
    {
        int pos = e.Position;
        if(pos < 0) { return; }

        Point point = new Point(
            x: scintilla.PointXFromPosition(pos),
            y: scintilla.PointYFromPosition(pos));

        //var apiInfo = await getAPIInfoAsync(scintilla.CurrentPosition);

    }
}
