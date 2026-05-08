namespace WinFormsTest.Demos;

/// <summary>
/// Demonstrates the RTF-based script editor.
/// </summary>
public partial class FormRTFDemo : Form
{
    private bool _isRunningOrCompiling;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormRTFDemo"/> class.
    /// </summary>
    public FormRTFDemo()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the editor when the form loads.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        rtfScriptEditor.Environment = CDS.CSharpScript2.ScriptEnvironment.Default;
        rtfScriptEditor.Script = @"System.Drawing.Point p = System.Drawing.Point.Empty;";
    }

    /// <summary>
    /// Executes a script-related action with consistent UI state management and exception handling.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns><see langword="true"/> when the action completed successfully; otherwise, <see langword="false"/>.</returns>
    private async Task<bool> PerformScriptActionAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_isRunningOrCompiling)
        {
            return false;
        }

        Enabled = false;
        _isRunningOrCompiling = true;
        outputPanel.Clear();

        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            outputPanel.AppendLine($"Error: {ex.Message}");
            return false;
        }
        finally
        {
            Enabled = true;
            _isRunningOrCompiling = false;
        }
    }

    /// <summary>
    /// Handles the Run button click event.
    /// </summary>
    private async void btnRun_Click(object sender, EventArgs e)
    {
        using var consoleHook = new CDS.CSharpScript2.Output.ScriptConsoleRedirect(text => outputPanel.Append(text ?? string.Empty));

        await PerformScriptActionAsync(async () =>
        {
            var compiled = await rtfScriptEditor.CompileAsync();
            await compiled.RunAsync();
        });
    }

    /// <summary>
    /// Handles the Compile button click event.
    /// </summary>
    private async void btnCompile_Click(object sender, EventArgs e)
    {
        await PerformScriptActionAsync(async () =>
        {
            var compiled = await rtfScriptEditor.CompileAsync();
            var output = compiled.CompilationOutput;

            foreach (var message in output.Messages)
            {
                outputPanel.AppendLine(message);
            }

            outputPanel.AppendLine($"{output.WarningCount} warnings and {output.ErrorCount} errors.");
        });
    }
}
