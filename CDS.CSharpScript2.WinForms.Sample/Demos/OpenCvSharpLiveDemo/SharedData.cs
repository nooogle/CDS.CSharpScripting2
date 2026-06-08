using OpenCvSharp;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.OpenCvSharpLiveDemo;

/// <summary>
/// Globals object passed to the user script on every captured frame.
/// <see cref="Source"/> holds the webcam frame; the script writes its result to <see cref="Dest"/>.
/// </summary>
public sealed class SharedData : IDisposable
{
    private bool _isDisposed;

    /// <summary>Gets or sets the source frame captured from the webcam (read-only for scripts).</summary>
    public Mat Source { get; set; } = new Mat();

    /// <summary>Gets or sets the destination frame the script should write its output into.</summary>
    public Mat Dest { get; set; } = new Mat();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed) { return; }

        Source.Dispose();
        Dest.Dispose();

        _isDisposed = true;
    }
}
