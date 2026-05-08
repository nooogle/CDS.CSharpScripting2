using CDS.CSharpScript2;

namespace ConsoleTest.Demos;

/// <summary>
/// Demonstrates a basic function where the script itself contains all the inputs and returns an output.
/// </summary>
class BasicDemo
{
    public static string Name => "Basic function";
    public static string Description => "A basic test where the script itself contains all the inputs and returns an output";

    public static void Run()
    {
        new BasicDemo().RunAsync().Wait();
    }

    private async Task RunAsync()
    {
        var logger = new TimedConsoleLogger();
        int x = 10;
        var script = $"return 2 * {x};";

        logger.Log("Creating script context");
        var context = await ScriptContext.CreateAsync();

        logger.Log("Applying script");
        context = context.ApplyScript(script);

        logger.Log("Compiling and running script");
        var executable = await new ScriptExecutor(context).CompileAsync<int>();
        int result = await executable.RunAsync<int>();

        logger.Log($"Script: {script}");
        logger.Log($"Result: {result}");
    }
}
