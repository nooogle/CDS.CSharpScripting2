using CDS.CSharpScript2;
using OpenCvSharp;

namespace ConsoleTest.Demos;

public class OpenCvSharpDemo
{
    public class SharedData
    {
        public Mat? Source { get; set; }
        public Mat? Dest { get; set; } = new Mat();
    }

    public static string Name => "OpenCvSharp demo";
    public static string Description => "Demonstrates using OpenCvSharp in a script";

    public static void Run()
    {
        new OpenCvSharpDemo().RunAsync().Wait();
    }

    private async Task RunAsync()
    {
        var logger = new TimedConsoleLogger();
        logger.Log("Loading image");
        var sharedData = new SharedData();
        sharedData.Source = Cv2.ImRead("Demos/IMG_1412.jpeg", ImreadModes.Grayscale);

        var script = "Cv2.Canny(Source, Dest!, 100, 200);";

        logger.Log("Setting up script environment");
        var scriptEnvironment =
            ScriptEnvironment
            .Default
            .WithGlobalType(typeof(SharedData))
            .WithAdditionalNamespaceForType<Mat>()
            .WithAdditionalReferenceForType<Mat>();

        logger.Log("Creating script context");
        var context = await ScriptContext.CreateAsync(scriptEnvironment);
        context = context.ApplyScript(script);

        logger.Log("Compiling script");
        var executable = await new ScriptExecutor(context).CompileAsync();

        logger.Log($"Running script: {script}");
        await executable.RunAsync(sharedData);

        logger.Log("Displaying images");
        Cv2.ImShow("Image", sharedData.Source!);
        Cv2.WaitKey(0);
        Cv2.ImShow("Edges", sharedData.Dest!);
        Cv2.WaitKey(0);
        Cv2.DestroyAllWindows();
    }
}
