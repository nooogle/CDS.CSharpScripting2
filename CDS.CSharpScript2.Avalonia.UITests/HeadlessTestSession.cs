using Avalonia;
using Avalonia.Headless;
using CDS.CSharpScript2.Avalonia.Sample;

// One headless Avalonia dispatcher serves the whole assembly, so tests must not run in parallel.
[assembly: DoNotParallelize]

namespace CDS.CSharpScript2.Avalonia.UITests;

/// <summary>
/// Owns the assembly-wide Avalonia.Headless session that every UI test runs inside.
/// </summary>
[TestClass]
public static class HeadlessTestSession
{
    private static HeadlessUnitTestSession? s_session;

    /// <summary>
    /// Builds the sample's <see cref="App"/> on the headless platform with Skia rendering, so
    /// <c>CaptureRenderedFrame</c> produces real pixels. Called by <see cref="HeadlessUnitTestSession"/>.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }

    /// <summary>Starts the headless session before any test runs.</summary>
    [AssemblyInitialize]
    public static void Start(TestContext context)
    {
        s_session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestSession));
    }

    /// <summary>Shuts the headless session down after the last test.</summary>
    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        if (s_session is { } session)
        {
            s_session = null;
            await session.DisposeAsync();
        }
    }

    /// <summary>
    /// Runs <paramref name="test"/> on the headless UI thread and waits for it to finish.
    /// </summary>
    public static Task RunAsync(Func<Task> test)
    {
        var session = s_session ?? throw new InvalidOperationException("Headless session has not started.");

        // Deliberately the Func<Task<TResult>> overload. Passing a Func<Task> straight through
        // binds to Dispatch<TResult>(Func<TResult>) with TResult = Task, which completes as soon
        // as the test *starts* — every test would "pass" without its assertions ever running.
        return session.Dispatch(
            async () =>
            {
                await test();
                return true;
            },
            CancellationToken.None);
    }
}
