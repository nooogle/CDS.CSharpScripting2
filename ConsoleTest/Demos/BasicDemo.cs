using CDS.CSScripting2;

namespace ConsoleTest.Demos;

/// <summary>
/// Demonstrates a basic function where the script itself contains all the inputs and returns an output.
/// </summary>
class BasicDemo
{
    /// <summary>
    /// Gets the name of the demo.
    /// </summary>
    public static string Name => "Basic function";

    /// <summary>
    /// Gets the description of the demo.
    /// </summary>
    public static string Description => "A basic test where the script itself contains all the inputs and returns an output";


    /// <summary>
    /// Runs the demo.
    /// </summary>
    public static void Run()
    {
        new BasicDemo().RunAsync().Wait();
    }


    /// <summary>
    /// Runs the demo.
    /// </summary>
    private async Task RunAsync()
    {
        // Setup
        var logger = new TimedConsoleLogger();
        int x = 10;
        var script = $"return 2 * {x};";

        // Create the script manager
        logger.Log("Creating script manager");
        var scriptManager = await ScriptManager.CreateAsync();

        // Apply the script
        logger.Log("Applying script");
        scriptManager = scriptManager.ApplyScript(script);

        // Run the script
        logger.Log("Running script");
        int result = await scriptManager.RunAsync<int>();

        // Output
        logger.Log("Output...");
        logger.Log($"Script: {script}");
        logger.Log($"Result: {result}");
    }
}
