using System.ComponentModel;
using System.Diagnostics;

namespace WinFormsTest.Demos.GlobalsDemo;

public partial class FormGlobals : Form
{
    /// <summary>
    /// Globals class for the demo
    /// </summary>
    public class Globals
    {
        public string Animal { get; set; } = "Cat";
    }


    private Settings settings;
    private CDS.CSScripting2.Editors.EditorManager? editorManager;
    private Globals globals = new Globals();

    public FormGlobals(Settings settings)
    {
        InitializeComponent();
        this.settings = settings;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        InitialiseEditor();
        propertyGrid1.SelectedObject = globals;
    }

    private void InitialiseEditor()
    {
        var env =
            CDS.CSScripting2.Env.Default
            .WithDrawingReferences()
            .WithGlobalType(typeof(Globals));

        editorManager = new CDS.CSScripting2.Editors.EditorManager(
            environment: env,
            scintillaScriptEditor.ApplyDiagnostics,
            scintillaScriptEditor.ApplySyntaxElements);

        scintillaScriptEditor.SetProcessScriptHandler(editorManager.ProcessScript);

        scintillaScriptEditor.Script = settings.Script;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (isRunningOrCompiling)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
        settings.Script = scintillaScriptEditor.Script;
    }

    private bool isRunningOrCompiling = false;


    private async Task PerformScriptManagerActions(Func<Task> action)
    {
        if (isRunningOrCompiling) { return; }

        groupBoxScript.Enabled = false;
        isRunningOrCompiling = true;

        outputPanel.Clear();
        await action();

        groupBoxScript.Enabled = true;
        isRunningOrCompiling = false;
    }

    private async void btnRun_Click(object sender, EventArgs e)
    {
        await PerformScriptManagerActions(async () =>
        {
            using var consoleHook = new CDS.CSScripting2.ConsoleOutputHook(outputPanel.Append);

            await editorManager!.RunAsync(globals);
            propertyGrid1.Refresh();
        });
    }

    private async void btnCompile_Click(object sender, EventArgs e)
    {
        await PerformScriptManagerActions(async () =>
        {
            var compilationOutput = await editorManager!.CompileAsync();

            outputPanel.AppendLine("Compilation complete");

            foreach (var message in compilationOutput.Messages)
            {
                outputPanel.AppendLine(message);
            }

            outputPanel.AppendLine($"\t{compilationOutput.WarningCount} warnings" + Environment.NewLine);
            outputPanel.AppendLine($"\t{compilationOutput.ErrorCount} errors" + Environment.NewLine);
        });
    }
}
