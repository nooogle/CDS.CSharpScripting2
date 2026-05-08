using System.ComponentModel;
using System.Diagnostics;

namespace WinFormsTest.Demos.GlobalsDemo;

public partial class FormGlobals : Form
{
    private readonly Settings settings;
    private readonly SharedData sharedData = new SharedData();
    private bool isRunningOrCompilingSentry;

    public FormGlobals(Settings settings)
    {
        InitializeComponent();
        this.settings = settings;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var env = CDS.CSharpScript2.ScriptEnvironment.Default
            .WithDrawingReferences()
            .WithGlobalType(typeof(SharedData));

        scintillaScriptEditor.Environment = env;
        scintillaScriptEditor.Script = settings.Script;

        propertyGrid1.SelectedObject = sharedData;
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

    private async Task PerformScriptAction(Func<Task> action)
    {
        if (isRunningOrCompilingSentry) return;

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
        using var consoleHook = new CDS.CSharpScript2.Output.ScriptConsoleRedirect(outputPanel.Append);

        await PerformScriptAction(async () =>
        {
            var compiled = await scintillaScriptEditor.CompileAsync();
            await compiled.RunAsync(sharedData);
        });

        propertyGrid1.Refresh();
    }

    private async void btnCompile_Click(object sender, EventArgs e)
    {
        await PerformScriptAction(async () =>
        {
            var compiled = await scintillaScriptEditor.CompileAsync();
            var output = compiled.CompilationOutput;

            outputPanel.AppendLine("Compilation complete");

            foreach (var message in output.Messages)
                outputPanel.AppendLine(message);

            outputPanel.AppendLine($"\t{output.WarningCount} warnings");
            outputPanel.AppendLine($"\t{output.ErrorCount} errors");
        });
    }
}
