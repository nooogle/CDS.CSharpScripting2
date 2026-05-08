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
    private readonly Settings settings;
    private readonly SharedData sharedData = new SharedData();
    private bool isRunningOrCompilingSentry;

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
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var env = CDS.CSharpScript2.ScriptEnvironment.Default
            .WithDrawingReferences()
            .WithGlobalType(typeof(SharedData))
            .WithAdditionalNamespaceForType<Mat>()
            .WithAdditionalReferenceForType<Mat>();

        scintillaScriptEditor.Environment = env;
        scintillaScriptEditor.Script = settings.Script;

        sharedData.Source = Cv2.ImRead($"{nameof(Demos)}/{nameof(OpenCvSharpDemo)}/IMG_1412.jpeg", ImreadModes.Grayscale);
        ShowImages();
    }

    /// <summary>
    /// Handles the closing event of the form.
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
        sharedData?.Dispose();
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

    /// <summary>
    /// Handles the click event of the run button.
    /// </summary>
    private async void btnRun_Click(object sender, EventArgs e)
    {
        using var consoleHook = new CDS.CSharpScript2.Output.ScriptConsoleRedirect(outputPanel.Append);

        try
        {
            await PerformScriptAction(async () =>
            {
                var compiled = await scintillaScriptEditor.CompileAsync();
                await compiled.RunAsync(sharedData);
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
    private async void btnCompile_Click(object sender, EventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            outputPanel.AppendLine($"Error: {ex.Message}");
        }
    }

    private void ShowImages()
    {
        ShowImage(sharedData.Source, pictureBoxSource);
        ShowImage(sharedData.Dest, pictureBoxDest);
    }

    private void ShowImage(Mat image, PictureBox pictureBox)
    {
        pictureBox.Image = image.Empty() ? null : image.ToBitmap();
    }
}
