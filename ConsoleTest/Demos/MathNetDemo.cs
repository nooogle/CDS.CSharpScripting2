using CDS.CSScripting2;

namespace ConsoleTest.Demos;


/// <summary>
/// Shared data demo
/// </summary>
public class MathNetDemo
{
    /// <summary>
    /// Shared data object
    /// </summary>
    public class SharedData
    {
        public double n { get; set; }
    }


    /// <summary>
    /// Name of the demo
    /// </summary>
    public static string Name => "MathNet demo";


    /// <summary>
    /// Description of the demo
    /// </summary>
    public static string Description =>
        "Demonstrates how to provide a script with access to shared data and additional libraries. " +
        "For this example, we calculate the gamma of a value using MathNet.Numerics.";

    /// <summary>
    /// Runs the demo.
    /// </summary>
    public static void Run()
    {
        var demo = new MathNetDemo();
        demo.RunAsync().Wait();
    }


    /// <summary>
    /// Run the demo
    /// </summary>
    private async Task RunAsync()
    {
        // Create a timing logger, the shared data object
        var logger = new TimedConsoleLogger();
        SharedData sharedData = new SharedData();


        // Create the script
        var script =
            "using MathNet.Numerics; " +
            "return SpecialFunctions.Gamma(n);";


        // Set up the script environment. We customize the environment to include:
        // 1. The shared data object - to allow the script to access the input data from this test
        // 2. A reference to MathNet.Numerics.LinearAlgebra.Matrix<double> - to allow the script to types from MathNet

        logger.Log("Setting up script environment");
        
        var scriptEnvironment =
            ScriptEnvironment.Default
            .WithGlobalType(typeof(SharedData))
            .WithAdditionalReferenceForType<MathNet.Numerics.LinearAlgebra.Matrix<double>>();


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
        for (int n = 1; n < 9; n *= 2)
        {
            sharedData.n = n;
            var gamma = await scriptManager.RunAsync<double>(sharedData);
            logger.Log($"Gamma({n}) = {gamma:0.000}");
        }
    }
}
