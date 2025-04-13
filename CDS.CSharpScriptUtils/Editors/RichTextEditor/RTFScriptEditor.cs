using System.Collections.Immutable;

namespace CDS.CSharpScriptUtils.Editors.RichTextEditor;

public partial class RTFScriptEditor : UserControl, IEditor
{
    private ApplyScriptDelegateAsync processScriptAsync;
    private ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> lastDiagnostics = [];
    private string lastScript = "";
    private Font errorFont;
    private ToolTipManager toolTipManager;
    private int programmaticTextChangeSentryDepth = 0;
    private GetAutoCompleteListDelegateAsync getAutoCompleteListAsync;
    private GetAPIInfoDelegateAsync getAPIInfoAsync;


    public string Script
    {
        get => richTextBox.Text;
        set => richTextBox.Text = value;
    }

    public RTFScriptEditor()
    {
        InitializeComponent();

        errorFont = new Font(this.Font, newStyle: FontStyle.Underline);
        toolTipManager = new ToolTipManager(richTextBox, toolTip);
    }

    public void SetDelegates(
        ApplyScriptDelegateAsync processScriptAsync,
        GetAutoCompleteListDelegateAsync getAutoCompleteListAsync,
        GetAPIInfoDelegateAsync getAPIInfoAsync)
    {
        this.processScriptAsync = processScriptAsync;
        this.getAutoCompleteListAsync = getAutoCompleteListAsync;
        this.getAPIInfoAsync = getAPIInfoAsync;
    }


    public void ApplyDiagnostics(ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics)
    {
        programmaticTextChangeSentryDepth++;
        lastDiagnostics = diagnostics;

        foreach (var diagnostic in diagnostics)
        {
            if ((diagnostic.DefaultSeverity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error) ||
                (diagnostic.DefaultSeverity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning))
            {
                ApplyErrorOrWarningStyle(diagnostic);
            }
        }

        programmaticTextChangeSentryDepth--;
    }


    private void ApplyErrorOrWarningStyle(Microsoft.CodeAnalysis.Diagnostic diagnostic)
    {
        richTextBox.Select(start: diagnostic.Location.SourceSpan.Start, length: diagnostic.Location.SourceSpan.Length);
        richTextBox.SelectionFont = errorFont;
    }

    public void ApplySyntaxElements(ImmutableArray<Syntax.SyntaxElement> syntaxElements)
    {
        programmaticTextChangeSentryDepth++;

        foreach (var syntaxElement in syntaxElements)
        {
            if (Syntax.SyntaxKindToSimpleGenerator.Map.TryGetValue(syntaxElement.Kind, out var simpleSyntaxKind))
            {
                richTextBox.Select(start: syntaxElement.Span.Start, length: syntaxElement.Span.Length);
                richTextBox.SelectionBackColor = Color.Yellow;
            }
        }

        programmaticTextChangeSentryDepth--;
    }


    private void richTextBox_TextChanged(object sender, EventArgs e)
    {
        if (programmaticTextChangeSentryDepth > 0) { return; }

        lastDiagnostics = [];

        timerChangeMonitor.Stop();
        timerChangeMonitor.Start();

    }

    private async void richTextBox_MouseMove(object sender, MouseEventArgs e)
    {
        var charIndex = richTextBox.GetCharIndexFromPosition(e.Location);

        toolTipManager.HandleMouseMove(
            diagnostics: lastDiagnostics,
            apiInfo: null,
            characterPosition: charIndex);
    }

    private void timerChangeMonitor_Tick(object sender, EventArgs e)
    {
        timerChangeMonitor.Stop();

        if (lastScript != Script)
        {
            PerformLiveCompilationOfChangedScript();
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
        programmaticTextChangeSentryDepth++;

        richTextBox.SelectAll();
        richTextBox.SelectionFont = this.Font;
        richTextBox.SelectionBackColor = Color.White;

        programmaticTextChangeSentryDepth--;
    }
}
