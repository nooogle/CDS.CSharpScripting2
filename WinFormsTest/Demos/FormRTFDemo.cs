namespace WinFormsTest.Demos;

public partial class FormRTFDemo : Form
{
    public FormRTFDemo()
    {
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        rtfScriptEditor.Environment = CDS.CSharpScript2.ScriptEnvironment.Default;
        rtfScriptEditor.Script = @"System.Drawing.Point p = System.Drawing.Point.Empty;";
    }

    private async void btnRun_Click(object sender, EventArgs e)
    {
        using var consoleHook = new CDS.CSharpScript2.Output.ScriptConsoleRedirect(outputPanel.Append);
        var compiled = await rtfScriptEditor.CompileAsync();
        await compiled.RunAsync();
    }

    private async void btnCompile_Click(object sender, EventArgs e)
    {
        Enabled = false;
        outputPanel.Clear();

        var compiled = await rtfScriptEditor.CompileAsync();
        var output = compiled.CompilationOutput;

        foreach (var message in output.Messages)
            outputPanel.AppendLine(message);

        outputPanel.AppendLine($"{output.WarningCount} warnings and {output.ErrorCount} errors.");

        Enabled = true;
    }
}
