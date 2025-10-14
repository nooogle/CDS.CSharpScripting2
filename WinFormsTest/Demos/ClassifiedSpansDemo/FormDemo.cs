using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using System.ComponentModel;

namespace WinFormsTest.Demos.ClassifiedSpansDemo;

public partial class FormDemo : Form
{
    private CDS.CSharpScript2.Editors.EditorManager editorManager;
    private readonly Settings settings;
    private CDS.CSharpScript2.CompiledScript? compiledScript;

    public FormDemo(Settings settings)
    {
        InitializeComponent();
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        InitialiseScript();
    }

    private void InitialiseScript()
    {
        editorManager = new CDS.CSharpScript2.Editors.EditorManager(
            environment: CDS.CSharpScript2.ScriptEnvironment.Default,
            scintillaScriptEditor.ApplyDiagnostics,
            scintillaScriptEditor.ApplyClassifications);

        scintillaScriptEditor.SetDelegates(
            editorManager.ApplyScript,
            editorManager.GetAutoCompletions,
            editorManager.GetAPIInfo);

        scintillaScriptEditor.Script = settings.Script;

        //_ = RefreshTree();
    }



    /// <summary>
    /// Saves the current script to settings when the form is closing.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        settings.Script = scintillaScriptEditor.Script;
    }

    private async void scintillaScriptEditor_OnScriptChanged(object sender, EventArgs e)
    {
        await RefreshTree();
    }

    private async Task RefreshTree()
    {
        while (!editorManager.IsReady)
        {
            await Task.Delay(250);
        }

        compiledScript = await editorManager.GetCompiledScriptAsync();
        var classifiedSpans = compiledScript.ClassifiedSpans;

        var root = compiledScript.SyntaxTree.GetRoot();

        listViewInfo.Items.Clear();

        foreach (var classifiedSpan in classifiedSpans)
        {
            var span = new Microsoft.CodeAnalysis.Text.TextSpan(classifiedSpan.SpanStart, classifiedSpan.SpanLength);
            var spanText = compiledScript.SyntaxTree.GetText().GetSubText(span).ToString();

            var listItem = new ListViewItem(new string[]
            {
                classifiedSpan.Classification.Humanize(),
                classifiedSpan.SpanStart.ToString(),
                classifiedSpan.SpanLength.ToString(),
                spanText
            });

            listItem.Tag = classifiedSpan;
            listViewInfo.Items.Add(listItem);
        }
    }


    private void listViewInfo_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (listViewInfo.SelectedItems.Count > 0)
        {
            var selectedItem = listViewInfo.SelectedItems[0];
            if (selectedItem.Tag is CDS.CSharpScript2.Classification.ClassifiedSymbol classifiedSymbol)
            {
                scintillaScriptEditor.HighlightText(
                    classifiedSymbol.SpanStart, 
                    classifiedSymbol.SpanLength);
            }
        }
    }
}
