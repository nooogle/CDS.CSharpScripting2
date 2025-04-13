using System.ComponentModel;
using System.Diagnostics;

namespace WinFormsTest.Demos.GlobalsDemo;

public partial class FormGlobals : Form
{
    private Settings settings;
    private CDS.CSharpScriptUtils.Editors.EditorManager? editorManager;
    private SharedData sharedData = new SharedData();
    private bool isRunningOrCompilingSentry = false;

    public FormGlobals(Settings settings)
    {
        InitializeComponent();
        this.settings = settings;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        InitialiseEditor();
        propertyGrid1.SelectedObject = sharedData;
    }

    private void InitialiseEditor()
    {
        var env =
            CDS.CSharpScriptUtils.ScriptEnvironment.Default
            .WithDrawingReferences()
            .WithGlobalType(typeof(SharedData));

        editorManager = new CDS.CSharpScriptUtils.Editors.EditorManager(
            environment: env,
            scintillaScriptEditor.ApplyDiagnostics,
            scintillaScriptEditor.ApplySyntaxElements);

        scintillaScriptEditor.SetDelegates(
            editorManager.ApplyScript,
            editorManager.GetAutoCompletions,
            editorManager.GetAPIInfo);

        scintillaScriptEditor.Script = settings.Script;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (isRunningOrCompilingSentry)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
        settings.Script = scintillaScriptEditor.Script;
    }
    private async Task PerformScriptManagerActions(Func<Task> action)
    {
        if (isRunningOrCompilingSentry) { return; }

        groupBoxScript.Enabled = false;
        isRunningOrCompilingSentry = true;

        outputPanel.Clear();
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();

        outputPanel.AppendLine($"Execution time: {stopwatch.ElapsedMilliseconds}ms");

        groupBoxScript.Enabled = true;
        isRunningOrCompilingSentry = false;
    }

    private async void btnRun_Click(object sender, EventArgs e)
    {
        using var consoleHook = new CDS.CSharpScriptUtils.ConsoleOutputHook(outputPanel.Append);

        await PerformScriptManagerActions(async () =>
        {
            await editorManager!.RunAsync(sharedData);
        });

        propertyGrid1.Refresh();
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

            outputPanel.AppendLine($"\t{compilationOutput.WarningCount} warnings");
            outputPanel.AppendLine($"\t{compilationOutput.ErrorCount} errors");
        });
    }
}
