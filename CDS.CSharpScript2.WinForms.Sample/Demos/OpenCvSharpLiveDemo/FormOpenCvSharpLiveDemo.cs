using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.OpenCvSharpLiveDemo;

/// <summary>
/// Demonstrates live webcam capture with per-frame C# script processing.
/// The script receives <see cref="SharedData.Source"/> (the raw frame) and should write its
/// result to <see cref="SharedData.Dest"/>. Both images are displayed with synchronised pan/zoom.
/// </summary>
public partial class FormOpenCvSharpLiveDemo : Form
{
    private readonly Settings _settings;
    private readonly SharedData _sharedData = new();

    private VideoCapture? _capture;
    private ExecutableScript? _executableScript;
    private bool _isFirstFrame;
    private bool _isProcessing;
    private bool _isChangingWebcamState;
    private string? _lastScriptErrorMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormOpenCvSharpLiveDemo"/> class.
    /// </summary>
    /// <param name="settings">Persisted settings for this demo.</param>
    public FormOpenCvSharpLiveDemo(Settings settings)
    {
        InitializeComponent();
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc/>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        scintillaScriptEditor.API.Environment = CreateScriptEnvironment();
        scintillaScriptEditor.API.Script = _settings.Script;

        zoomPictureBoxSource.ViewportChanged += OnSourceViewportChanged;

        // Fill the combo box with a list of available cameras using the camera names
        var cameraList = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            using var tempCapture = new VideoCapture(i);
            if (tempCapture.IsOpened())
            {
                cameraList.Add($"Camera {i}");

                comboBoxCameras.Items.Add($"Camera {i}");
            }
        }

        if (comboBoxCameras.Items.Count > 0)
        {
            comboBoxCameras.SelectedIndex = 0;
        }
    }

    /// <inheritdoc/>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (chkWebcamRunning.Checked)
        {
            StopWebcam();
        }

        base.OnFormClosing(e);
        _settings.Script = scintillaScriptEditor.API.Script;
        _sharedData.Dispose();
    }

    // ── Script environment ────────────────────────────────────────────────────

    private static CDS.CSharpScript2.ScriptEnvironment CreateScriptEnvironment()
    {
        return CDS.CSharpScript2.ScriptEnvironment.Default
            .WithDrawingReferences()
            .WithGlobalType(typeof(SharedData))
            .WithAdditionalNamespaceForType<Mat>()
            .WithAdditionalReferenceForType<Mat>();
    }

    // ── Viewport sync ─────────────────────────────────────────────────────────

    private void OnSourceViewportChanged(object? sender, EventArgs e)
    {
        zoomPictureBoxDest.SetViewport(zoomPictureBoxSource.Zoom, zoomPictureBoxSource.Pan);
    }

    // ── Webcam control ────────────────────────────────────────────────────────

    private async void chkWebcamRunning_CheckedChanged(object sender, EventArgs e)
    {
        if (_isChangingWebcamState) { return; }

        if (chkWebcamRunning.Checked)
        {
            await StartWebcamAsync();
        }
        else
        {
            StopWebcam();
        }
    }

    private async Task StartWebcamAsync()
    {
        outputPanel.Clear();
        outputPanel.AppendLine("Compiling script...");

        _executableScript = await scintillaScriptEditor.API.CompileAsync();

        if (_executableScript.HasErrors)
        {
            foreach (var msg in _executableScript.CompilationOutput.Messages)
            {
                outputPanel.AppendLine(msg);
            }

            outputPanel.AppendLine($"Compilation failed — {_executableScript.CompilationOutput.ErrorCount} error(s). Fix the script before starting the webcam.");
            SetWebcamCheckbox(isChecked: false);
            return;
        }

        _capture = new VideoCapture(comboBoxCameras.SelectedIndex);

        if (!_capture.IsOpened())
        {
            outputPanel.AppendLine($"Could not open webcam at device index {comboBoxCameras.SelectedIndex}.");
            _capture.Dispose();
            _capture = null;
            SetWebcamCheckbox(isChecked: false);
            return;
        }

        _isFirstFrame = true;
        _lastScriptErrorMessage = null;
        scintillaScriptEditor.Enabled = false;
        comboBoxCameras.Enabled = false;
        captureTimer.Start();
        outputPanel.AppendLine("Webcam running. Double-click either image to reset zoom.");
    }

    private void StopWebcam()
    {
        captureTimer.Stop();
        _capture?.Dispose();
        _capture = null;
        scintillaScriptEditor.Enabled = true;
        comboBoxCameras?.Enabled = true;
        outputPanel.AppendLine("Webcam stopped.");
    }

    /// <summary>
    /// Changes the checkbox state without re-entering <see cref="chkWebcamRunning_CheckedChanged"/>.
    /// </summary>
    private void SetWebcamCheckbox(bool isChecked)
    {
        _isChangingWebcamState = true;
        try
        {
            chkWebcamRunning.Checked = isChecked;
        }
        finally
        {
            _isChangingWebcamState = false;
        }
    }

    // ── Frame capture loop ────────────────────────────────────────────────────

    private async void captureTimer_Tick(object sender, EventArgs e)
    {
        if (_isProcessing) { return; }

        _isProcessing = true;

        try
        {
            await ProcessFrameAsync();
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task ProcessFrameAsync()
    {
        if (_capture == null || _executableScript == null) { return; }

        var frame = new Mat();

        if (!_capture.Read(frame))
        {
            frame.Dispose();
            return;
        }

        _sharedData.Source.Dispose();
        _sharedData.Dest.Dispose();
        _sharedData.Source = frame;
        _sharedData.Dest = new Mat();

        try
        {
            await _executableScript.RunAsync(_sharedData);
            _lastScriptErrorMessage = null;
        }
        catch (Exception ex)
        {
            // Throttle error output: show only when the message changes.
            if (ex.Message != _lastScriptErrorMessage)
            {
                _lastScriptErrorMessage = ex.Message;
                outputPanel.AppendLine($"Script error: {ex.Message}");
            }
        }

        UpdateImages();

        if (_isFirstFrame)
        {
            _isFirstFrame = false;
            zoomPictureBoxSource.FitToControl();
            // Dest follows via ViewportChanged → OnSourceViewportChanged.
        }
    }

    // ── Image display ─────────────────────────────────────────────────────────

    private void UpdateImages()
    {
        zoomPictureBoxSource.Image = _sharedData.Source.Empty() ? null : _sharedData.Source.ToBitmap();
        zoomPictureBoxDest.Image = _sharedData.Dest.Empty() ? null : _sharedData.Dest.ToBitmap();
    }

    private void tableLayoutPanelLeft_Paint(object sender, PaintEventArgs e)
    {

    }
}
