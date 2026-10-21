using System.Runtime.InteropServices;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// Stops a control repainting while it is rebuilt, so a page of many formatted runs is drawn once
/// rather than once per run. Dispose to resume painting and redraw.
/// </summary>
internal readonly struct RedrawSuspender : IDisposable
{
    private const int WmSetRedraw = 0x000B;

    private readonly Control _control;

    public RedrawSuspender(Control control)
    {
        _control = control;
        if (control.IsHandleCreated)
        {
            SendMessage(control.Handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public void Dispose()
    {
        if (_control.IsHandleCreated)
        {
            SendMessage(_control.Handle, WmSetRedraw, new IntPtr(1), IntPtr.Zero);
            _control.Invalidate(true);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
