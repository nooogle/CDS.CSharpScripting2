using System.Diagnostics;

namespace WinFormsTest.Demos;

public partial class FormRTFDemo : Form
{
    private CDS.CSharpScript2.Editors.EditorManager? editorManager;

    public FormRTFDemo()
    {
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        InitialiseEditor();
    }


    private void InitialiseEditor()
    {
        editorManager = new CDS.CSharpScript2.Editors.EditorManager(
            environment: CDS.CSharpScript2.ScriptEnvironment.Default,
            rtfScriptEditor.ApplyDiagnostics,
            rtfScriptEditor.ApplySyntaxElements);

        rtfScriptEditor.SetDelegates(
            editorManager.ApplyScript,
            editorManager.GetAutoCompletions,
            editorManager.GetAPIInfo);

        rtfScriptEditor.Script = @"System.Drawing.Point p = System.Drawing.Point.Empty;";
    }

    private async void btnRun_Click(object sender, EventArgs e)
    {
        await editorManager!.RunAsync();
    }


    private async void btnCompile_Click(object sender, EventArgs e)
    {
        Enabled = false;
        outputPanel.Clear();

        var compilationOutput = await editorManager!.CompileAsync();

        foreach (var message in compilationOutput.Messages)
        {
            outputPanel.AppendLine(message);
        }

        outputPanel.AppendLine(
            $"{compilationOutput.WarningCount} warnings and " +
            $"{compilationOutput.ErrorCount} errors.");

        Enabled = true;
    }
}
