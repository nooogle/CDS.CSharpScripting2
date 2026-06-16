using System.ComponentModel;
using System.Diagnostics;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.GlobalsDemo;

/// <summary>
/// Provides a demo form for working with globals shared between the host and the script.
/// </summary>
public partial class FormGlobals : Form
{
    private readonly Settings _settings;
    private readonly SharedData _sharedData = new();
    private bool _isRunningOrCompiling;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormGlobals"/> class.
    /// </summary>
    /// <param name="settings">The settings for the demo.</param>
    public FormGlobals(Settings settings)
    {
        InitializeComponent();
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Initializes the editor and shared data when the form loads.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var environment = CDS.CSharpScript2.ScriptEnvironment.Default
            .WithDrawingReferences()
            .WithGlobalType(typeof(SharedData));

        scintillaScriptEditor.API.Environment = environment;
        scintillaScriptEditor.API.Script = _settings.Script;

        propertyGrid1.SelectedObject = _sharedData;
    }

    /// <summary>
    /// Saves the current script when the form is closing.
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isRunningOrCompiling)
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
        _settings.Script = scintillaScriptEditor.API.Script;
    }

    /// <summary>
    /// Executes a script-related action with consistent UI state management and exception handling.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns><see langword="true"/> when the action completed successfully; otherwise, <see langword="false"/>.</returns>
    private async Task<bool> PerformScriptActionAsync(Func<Task> action)
    {
        if (action == null) { throw new ArgumentNullException(nameof(action)); }

        if (_isRunningOrCompiling)
        {
            return false;
        }

        groupBoxScript.Enabled = false;
        _isRunningOrCompiling = true;

        outputPanel.Clear();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await action();
            stopwatch.Stop();
            outputPanel.AppendLine($"Execution time: {stopwatch.ElapsedMilliseconds}ms");
            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            outputPanel.AppendLine($"Error: {ex.Message}");
            return false;
        }
        finally
        {
            groupBoxScript.Enabled = true;
            _isRunningOrCompiling = false;
        }
    }

    /// <summary>
    /// Handles the Run button click event.
    /// </summary>
    private async void btnRun_Click(object sender, EventArgs e)
    {
        using var consoleHook = new CDS.CSharpScript2.Output.ScriptConsoleRedirect(text => outputPanel.Append(text ?? string.Empty));

        var didSucceed = await PerformScriptActionAsync(async () =>
        {
            var compiled = await scintillaScriptEditor.API.CompileAsync();
            await compiled.RunAsync(_sharedData);
        });

        if (didSucceed)
        {
            propertyGrid1.Refresh();
        }
    }

    /// <summary>
    /// Handles the Compile button click event.
    /// </summary>
    private async void btnCompile_Click(object sender, EventArgs e)
    {
        await PerformScriptActionAsync(async () =>
        {
            var compiled = await scintillaScriptEditor.API.CompileAsync();
            var output = compiled.CompilationOutput;

            outputPanel.AppendLine("Compilation complete");

            foreach (var message in output.Messages)
            {
                outputPanel.AppendLine(message);
            }

            outputPanel.AppendLine($"\t{output.WarningCount} warnings");
            outputPanel.AppendLine($"\t{output.ErrorCount} errors");
        });
    }
}
