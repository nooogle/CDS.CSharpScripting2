using System.ComponentModel;

namespace WinFormsTest.Demos.BasicDemo;

/// <summary>
/// Provides a basic demonstration form for C# scripting functionality.
/// </summary>
public partial class FormBasicDemo : Form
{
    private readonly Settings settings;
    private CDS.CSharpScriptUtils.Editors.EditorManager? editorManager;
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
        InitialiseEditor();
    }

    /// <summary>
    /// Sets up the script editor and its associated manager.
    /// </summary>
    private void InitialiseEditor()
    {
        editorManager = new CDS.CSharpScriptUtils.Editors.EditorManager(
            environment: CDS.CSharpScriptUtils.ScriptEnvironment.Default,
            scintillaScriptEditor.ApplyDiagnostics,
            scintillaScriptEditor.ApplySyntaxElements);

        scintillaScriptEditor.SetDelegates(
            editorManager.ApplyScript,
            editorManager.GetAutoCompletions,
            editorManager.GetAPIInfo);

        scintillaScriptEditor.Script = settings.Script;
    }

    /// <summary>
    /// Saves the current script to settings when the form is closing.
    /// </summary>
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

    /// <summary>
    /// Executes a script manager action with proper state handling.
    /// </summary>
    /// <param name="action">The async action to perform.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task PerformScriptManagerActions(Func<Task> action)
    {
        if (isRunningOrCompilingSentry)
        {
            return;
        }

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
    /// Handles the Run button click event to execute the script.
    /// </summary>
    private async void btnRun_Click(object sender, EventArgs e)
    {
        if (editorManager == null)
        {
            outputPanel.AppendLine("Error: Editor not initialized");
            return;
        }

        await PerformScriptManagerActions(async () =>
        {
            using var consoleHook = new CDS.CSharpScriptUtils.ConsoleOutputHook(outputPanel.Append);
            await editorManager.RunAsync();
        });
    }

    /// <summary>
    /// Handles the Compile button click event to compile the script.
    /// </summary>
    private async void btnCompile_Click(object sender, EventArgs e)
    {
        if (editorManager == null)
        {
            outputPanel.AppendLine("Error: Editor not initialized");
            return;
        }

        await PerformScriptManagerActions(async () =>
        {
            var compilationOutput = await editorManager.CompileAsync();

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
