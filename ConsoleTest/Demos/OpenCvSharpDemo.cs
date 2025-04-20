using CDS.CSharpScript2;
using OpenCvSharp;

namespace ConsoleTest.Demos;


/// <summary>
/// Shared data demo
/// </summary>
public class OpenCvSharpDemo
{
    /// <summary>
    /// Shared data object
    /// </summary>
    public class SharedData
    {
        public Mat? Source { get; set; }
        public Mat? Dest { get; set; } = new Mat();
    }


    /// <summary>
    /// Name of the demo
    /// </summary>
    public static string Name => "OpenCvSharp demo";


    /// <summary>
    /// Description of the demo
    /// </summary>
    public static string Description => "Demonstrates using OpenCvSharp in a script";

    /// <summary>
    /// Runs the demo.
    /// </summary>
    public static void Run()
    {
        var demo = new OpenCvSharpDemo();
        demo.RunAsync().Wait();
    }


    /// <summary>
    /// Run the demo
    /// </summary>
    private async Task RunAsync()
    {
        // Create a timing logger and prepare the shared data
        var logger = new TimedConsoleLogger();
        logger.Log("Loading image");
        var sharedData = new SharedData();
        sharedData.Source = Cv2.ImRead("Demos/IMG_1412.jpeg", ImreadModes.Grayscale);


        // Setup the script
        var script = "Cv2.Canny(Source, Dest!, 100, 200);";


        // Set up the script environment. This is the default environment plus:
        // 1. Shared data
        // 2. OpenCvSharp
        logger.Log("Setting up script environment");
        var scriptEnvironment =
            ScriptEnvironment
            .Default
            .WithGlobalType(typeof(SharedData))
            .WithAdditionalNamespaceForType<Mat>()
            .WithAdditionalReferenceForType<Mat>();


        // Create the script manager
        logger.Log("Creating script manager");
        var scriptManager = await ScriptManager.CreateAsync(scriptEnvironment);


        // Apply the script to the script manager
        logger.Log("Applying script");
        scriptManager = scriptManager.ApplyScript(script);


        // Compile the script so that execution is faster
        logger.Log("Compiling script");
        await scriptManager.CompileAsync();


        // Run the script
        logger.Log($"Running script: {script}");
        await scriptManager.RunAsync(sharedData);


        // display the image
        logger.Log("Displaying images");
        Cv2.ImShow("Image", sharedData.Source!);
        Cv2.WaitKey(0);
        Cv2.ImShow("Edges", sharedData.Dest!);
        Cv2.WaitKey(0);
        Cv2.DestroyAllWindows();
    }
}
