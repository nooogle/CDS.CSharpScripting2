using CDS.CSharpScript2;

namespace ConsoleTest.Demos;

public class MathNetDemo
{
    public class SharedData
    {
        public double n { get; set; }
    }

    public static string Name => "MathNet demo";
    public static string Description =>
        "Demonstrates how to provide a script with access to shared data and additional libraries. " +
        "For this example, we calculate the gamma of a value using MathNet.Numerics.";

    public static void Run()
    {
        new MathNetDemo().RunAsync().Wait();
    }

    private async Task RunAsync()
    {
        var logger = new TimedConsoleLogger();
        SharedData sharedData = new SharedData();

        var script =
            "using MathNet.Numerics; " +
            "return SpecialFunctions.Gamma(n);";

        logger.Log("Setting up script environment");
        var scriptEnvironment =
            ScriptEnvironment.Default
            .WithGlobalType(typeof(SharedData))
            .WithAdditionalReferenceForType<MathNet.Numerics.LinearAlgebra.Matrix<double>>();

        logger.Log("Creating script context");
        var context = await ScriptContext.CreateAsync(scriptEnvironment);
        context = context.ApplyScript(script);

        logger.Log("Compiling script");
        var executable = await new ScriptExecutor(context).CompileAsync<double>();

        logger.Log($"Running script: {script}");
        for (int n = 1; n < 9; n *= 2)
        {
            sharedData.n = n;
            var gamma = await executable.RunAsync<double>(sharedData);
            logger.Log($"Gamma({n}) = {gamma:0.000}");
        }
    }
}
