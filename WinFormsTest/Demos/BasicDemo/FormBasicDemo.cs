using System.ComponentModel;

namespace WinFormsTest.Demos.BasicDemo;

/// <summary>
/// Provides a basic demonstration form for C# scripting functionality.
/// </summary>
public partial class FormBasicDemo : Form
{
    private readonly Settings settings;
    private bool isRunningOrCompilingSentry;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormBasicDemo"/> class.
    /// </summary>
    /// <param name="settings">The settings to use for this demo.</param>
    public FormBasicDemo(Settings settings)
    {
        InitializeComponent();
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Initializes the editor when the form loads.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        scintillaScriptEditor.Environment = CDS.CSharpScript2.ScriptEnvironment.Default;
        scintillaScriptEditor.Script = settings.Script;
    }


    /// <summary>
    /// Saves the current script to settings when the form is closing.
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (isRunningOrCompilingSentry)
        {
            e.Cancel = true;
            return;
        }

        settings.Script = scintillaScriptEditor.Script;

        base.OnFormClosing(e);
    }

    /// <summary>
    /// Executes a script manager action with proper state handling.
    /// </summary>
    private async Task PerformScriptAction(Func<Task> action)
    {
        if (isRunningOrCompilingSentry) return;

        try
        {
            isRunningOrCompilingSentry = true;
            outputPanel.Clear();
            await action();
        }
        catch (Exception ex)
        {
            outputPanel.AppendLine($"Error: {ex.Message}");
        }
        finally
        {
            isRunningOrCompilingSentry = false;
        }
    }

    /// <summary>
    /// Handles the Run button click event to compile and execute the script.
    /// </summary>
    private async void btnRun_Click(object sender, EventArgs e)
    {
        await PerformScriptAction(async () =>
        {
            using var consoleHook = new CDS.CSharpScript2.Output.ScriptConsoleRedirect(text => outputPanel.Append(text ?? string.Empty));
            var compiled = await scintillaScriptEditor.CompileAsync();
            await compiled.RunAsync();
        });
    }

    /// <summary>
    /// Handles the Compile button click event to compile the script.
    /// </summary>
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
