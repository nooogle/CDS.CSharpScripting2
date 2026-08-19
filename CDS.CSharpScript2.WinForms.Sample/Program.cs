namespace CDS.CSharpScript2.WinForms.Sample;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
#if NET48
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
#else
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
#endif
        Application.Run(CreateMainForm(args));
    }

    /// <summary>
    /// Chooses the form to show at startup. The <c>--demo=basic</c> switch opens
    /// <see cref="Demos.BasicDemo.FormBasicDemo"/> directly, bypassing FormMain's demo picker —
    /// used by UI-automation tests that only need to drive the Basic demo itself.
    /// </summary>
    private static Form CreateMainForm(string[] args)
    {
        if (Array.Exists(args, a => string.Equals(a, "--demo=basic", StringComparison.OrdinalIgnoreCase)))
        {
            return new Demos.BasicDemo.FormBasicDemo(new Demos.BasicDemo.Settings());
        }

        return new FormMain();
    }
}
