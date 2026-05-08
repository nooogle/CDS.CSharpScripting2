using CDS.CSharpScript2;

namespace ConsoleTest.Demos;

public class SharedDataDemo
{
    public class SharedData
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public static string Name => "Shared data";
    public static string Description =>
        "A basic test where the script is given access to shared data for the inputs and outputs. It is also " +
        "called multiple times. Compilation only occurs the first time the script is called. The compiled script is then reused for " +
        "successive calls.";

    public static void Run()
    {
        new SharedDataDemo().RunAsync().Wait();
    }

    private async Task RunAsync()
    {
        var logger = new TimedConsoleLogger();
        SharedData sharedData = new SharedData();
        var script = "Y = Math.Pow(X, 2);";

        logger.Log("Setting up script environment");
        var scriptEnvironment = ScriptEnvironment.Default.WithGlobalType(typeof(SharedData));

        logger.Log("Creating script context");
        var context = await ScriptContext.CreateAsync(scriptEnvironment);
        context = context.ApplyScript(script);

        logger.Log("Compiling script");
        var executable = await new ScriptExecutor(context).CompileAsync();

        logger.Log($"Running script: {script}");
        for (int i = 0; i < 5; i++)
        {
            sharedData.X = i;
            await executable.RunAsync(sharedData);
            logger.Log($"X: {sharedData.X}, Y: {sharedData.Y}");
        }
    }
}
