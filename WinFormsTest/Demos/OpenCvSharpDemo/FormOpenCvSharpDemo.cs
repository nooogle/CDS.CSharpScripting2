using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.ComponentModel;
using System.Diagnostics;

namespace WinFormsTest.Demos.OpenCvSharpDemo;

/// <summary>
/// Form for demonstrating OpenCvSharp functionality.
/// </summary>
public partial class FormOpenCvSharpDemo : Form
{
    private Settings settings;
    private CDS.CSharpScriptUtils.Editors.EditorManager? editorManager;
    private SharedData sharedData = new SharedData();
    private bool isRunningOrCompilingSentry = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormOpenCvSharpDemo"/> class.
    /// </summary>
    /// <param name="settings">The settings for the demo.</param>
    public FormOpenCvSharpDemo(Settings settings)
    {
        InitializeComponent();
        this.settings = settings;
    }

    /// <summary>
    /// Handles the load event of the form.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        InitialiseEditor();

        sharedData.Source = Cv2.ImRead($"{nameof(Demos)}/{nameof(OpenCvSharpDemo)}/IMG_1412.jpeg", ImreadModes.Grayscale);
        ShowImages();
    }

    private void ShowImages()
    {
        ShowImage(sharedData.Source, pictureBoxSource);
        ShowImage(sharedData.Dest, pictureBoxDest);
    }

    private void ShowImage(Mat image, PictureBox pictureBox)
    {
        pictureBox.Image =
            image.Empty() ?
            null :
            image.ToBitmap();
    }

    private void InitialiseEditor()
    {
        var env =
            CDS.CSharpScriptUtils.ScriptEnvironment.Default
            .WithDrawingReferences()
            .WithGlobalType(typeof(SharedData))
            .WithAdditionalNamespaceForType<Mat>()
            .WithAdditionalReferenceForType<Mat>();

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

    /// <summary>
    /// Handles the closing event of the form.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (isRunningOrCompilingSentry)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
        settings.Script = scintillaScriptEditor.Script;
        sharedData?.Dispose();
    }


    /// <summary>
    /// Performs the script manager actions.
    /// </summary>
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

    /// <summary>
    /// Handles the click event of the run button.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void btnRun_Click(object sender, EventArgs e)
    {
        using var consoleHook = new CDS.CSharpScriptUtils.ConsoleOutputHook(outputPanel.Append);

        try
        {
            await PerformScriptManagerActions(async () =>
            {
                await editorManager!.RunAsync(sharedData);
            });

            ShowImages();
        }
        catch (Exception ex)
        {
            outputPanel.AppendLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the click event of the compile button.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void btnCompile_Click(object sender, EventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            outputPanel.AppendLine($"Error: {ex.Message}");
        }
    }
}
