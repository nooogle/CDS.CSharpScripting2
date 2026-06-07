using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.ComponentModel;
using System.Diagnostics;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.OpenCvSharpStaticDemo;

/// <summary>
/// Form for demonstrating OpenCvSharp functionality.
/// </summary>
public partial class FormOpenCvSharpDemo : Form
{
    private readonly Settings _settings;
    private readonly SharedData _sharedData = new();
    private bool _isRunningOrCompiling;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormOpenCvSharpDemo"/> class.
    /// </summary>
    /// <param name="settings">The settings for the demo.</param>
    public FormOpenCvSharpDemo(Settings settings)
    {
        InitializeComponent();
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Handles the load event of the form.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var environment = CreateScriptEnvironment();

        scintillaScriptEditor.Environment = environment;
        scintillaScriptEditor.Script = _settings.Script;

        _sharedData.Source = Cv2.ImRead($"{nameof(Demos)}/{nameof(OpenCvSharpStaticDemo)}/IMG_1412.jpeg", ImreadModes.Grayscale);
        ShowImages();
    }

    /// <summary>
    /// Handles the form-closing event.
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isRunningOrCompiling)
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
        _settings.Script = scintillaScriptEditor.Script;
        _sharedData.Dispose();
    }

    /// <summary>
    /// Creates the scripting environment used by the demo.
    /// </summary>
    /// <returns>The configured script environment.</returns>
    private static CDS.CSharpScript2.ScriptEnvironment CreateScriptEnvironment()
    {
        return CDS.CSharpScript2.ScriptEnvironment.Default
            .WithDrawingReferences()
            .WithGlobalType(typeof(SharedData))
            .WithAdditionalNamespaceForType<Mat>()
            .WithAdditionalReferenceForType<Mat>();
    }

    /// <summary>
    /// Executes a script-related action with consistent UI state management and exception handling.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns><see langword="true"/> when the action completed successfully; otherwise, <see langword="false"/>.</returns>
    private async Task<bool> PerformScriptActionAsync(Func<Task> action)
    {
        if(action == null) { throw new ArgumentNullException(nameof(action)); }

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
    /// Handles the click event of the run button.
    /// </summary>
    private async void btnRun_Click(object sender, EventArgs e)
    {
        using var consoleHook = new CDS.CSharpScript2.Output.ScriptConsoleRedirect(text => outputPanel.Append(text ?? string.Empty));

        var didSucceed = await PerformScriptActionAsync(async () =>
        {
            var compiled = await scintillaScriptEditor.CompileAsync();
            await compiled.RunAsync(_sharedData);
        });

        if (didSucceed)
        {
            ShowImages();
        }
    }

    /// <summary>
    /// Handles the click event of the compile button.
    /// </summary>
    private async void btnCompile_Click(object sender, EventArgs e)
    {
        await PerformScriptActionAsync(async () =>
        {
            var compiled = await scintillaScriptEditor.CompileAsync();
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

    /// <summary>
    /// Refreshes the source and destination image previews.
    /// </summary>
    private void ShowImages()
    {
        ShowImage(_sharedData.Source, pictureBoxSource);
        ShowImage(_sharedData.Dest, pictureBoxDest);
    }

    /// <summary>
    /// Displays an image in the specified picture box.
    /// </summary>
    /// <param name="image">The image to display.</param>
    /// <param name="pictureBox">The destination picture box.</param>
    private void ShowImage(Mat image, PictureBox pictureBox)
    {
        pictureBox.Image = image.Empty() ? null : image.ToBitmap();
    }
}
