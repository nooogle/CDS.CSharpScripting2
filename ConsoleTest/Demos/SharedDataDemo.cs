using CDS.CSScripting2;

namespace ConsoleTest.Demos;


/// <summary>
/// Shared data demo
/// </summary>
public class SharedDataDemo : IDemo
{
    /// <summary>
    /// Shared data object
    /// </summary>
    public class SharedData
    {
        public double X { get; set; }
        public double Y { get; set; }
    }


    /// <summary>
    /// Name of the demo
    /// </summary>
    public string Name => "Shared data";


    /// <summary>
    /// Description of the demo
    /// </summary>
    public string Description => "A basic test where the script is given access to shared data for the inputs and outputs. It is also " +
        "called multiple times. Compilication only occurs the first time the script is called. The compiled script is then reused for " +
        "successive calls.";


    /// <summary>
    /// Run the demo
    /// </summary>
    public async Task Run()
    {
        // Create a timing logger, the shared data object and the script.
        var logger = new TimedConsoleLogger();
        SharedData sharedData = new SharedData();
        var script = "Y = Math.Pow(X, 2);";

        // Set up the script environment. This is just the default environment
        // with the global type set to SharedData.
        logger.Log("Setting up script environment");
        var scriptEnvironment = ScriptEnvironment.Default.WithGlobalType(typeof(SharedData));

        // Create the script manager
        logger.Log("Creating script manager");
        var scriptManager = await ScriptManager.CreateAsync(scriptEnvironment);

        // Apply the script to the script manager
        logger.Log("Applying script");
        scriptManager = scriptManager.ApplyScript(script);

        // Compile the script so that execution is faster
        logger.Log("Compiling script");
        await scriptManager.CompileAsync();

        // Run the script multiple times with different inputs.
        logger.Log($"Running script: {script}");
        for (int i = 0; i < 5; i++)
        {
            sharedData.X = i;
            await scriptManager.RunAsync(sharedData);
            logger.Log($"X: {sharedData.X}, Y: {sharedData.Y}");
        }
    }
}
