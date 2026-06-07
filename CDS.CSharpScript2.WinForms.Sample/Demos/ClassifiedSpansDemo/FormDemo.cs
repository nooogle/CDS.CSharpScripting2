using Humanizer;
using Microsoft.CodeAnalysis;
using System.ComponentModel;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.ClassifiedSpansDemo;

public partial class FormDemo : Form
{
    private readonly Settings settings;

    public FormDemo(Settings settings)
    {
        InitializeComponent();
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        scintillaScriptEditor.Environment = CDS.CSharpScript2.ScriptEnvironment.Default;
        scintillaScriptEditor.Script = settings.Script;
        scintillaScriptEditor.ScriptChanged += async (_, _) => await RefreshList();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        settings.Script = scintillaScriptEditor.Script;
    }

    // ScriptChanged fires after analysis completes, so Manager is ready immediately.
    private async Task RefreshList()
    {
        var manager = scintillaScriptEditor.Manager;
        if (manager is null) return;

        var classifiedSpans = manager.LastClassifications;
        var syntaxTree = await manager.GetSyntaxTreeAsync();

        listViewInfo.Items.Clear();

        foreach (var classifiedSpan in classifiedSpans)
        {
            string spanText = string.Empty;

            if (syntaxTree != null)
            {
                var span = new Microsoft.CodeAnalysis.Text.TextSpan(classifiedSpan.SpanStart, classifiedSpan.SpanLength);
                spanText = syntaxTree.GetText().GetSubText(span).ToString();
            }

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
