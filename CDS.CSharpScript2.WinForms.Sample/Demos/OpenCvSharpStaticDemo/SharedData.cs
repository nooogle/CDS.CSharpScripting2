using OpenCvSharp;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.OpenCvSharpStaticDemo;


/// <summary>
/// Shared data for the demo
/// </summary>
public sealed class SharedData : IDisposable
{
    private bool isDisposed;

    public Mat Source { get; set; } = new Mat();

    public Mat Dest { get; set; } = new Mat();

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }
     
        Source.Dispose();
        Dest.Dispose();
        
        isDisposed = true;
    }
}
