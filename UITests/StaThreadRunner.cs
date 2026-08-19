using System.Runtime.ExceptionServices;

namespace UITests;

/// <summary>
/// Runs an action on a dedicated STA thread and rethrows any exception it raises on the calling
/// thread, preserving the original type and stack trace. UI Automation's COM interop (used by
/// FlaUI) needs an STA thread; MSTest does not guarantee one, so tests that drive FlaUI wrap their
/// body in <see cref="Run"/>.
/// </summary>
internal static class StaThreadRunner
{
    /// <summary>
    /// Runs <paramref name="action"/> on a new STA thread and blocks until it completes.
    /// </summary>
    /// <param name="action">The action to run.</param>
    public static void Run(Action action)
    {
        ExceptionDispatchInfo? capturedException = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                capturedException = ExceptionDispatchInfo.Capture(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        capturedException?.Throw();
    }
}
