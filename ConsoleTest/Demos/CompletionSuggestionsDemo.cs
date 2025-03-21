using CDS.CSScripting2;

namespace ConsoleTest.Demos;

class CompletionSuggestionsDemo
{
    /// <summary>
    /// Gets the name of the demo.
    /// </summary>
    public static string Name => "Completion suggestions";

    /// <summary>
    /// Gets the description of the demo.
    /// </summary>
    public static string Description => "TODO";

    /// <summary>
    /// Runs the demo.
    /// </summary>
    public static void Run()
    {
        var demo = new CompletionSuggestionsDemo();
        demo.RunAsync().Wait();
    }

    /// <summary>
    /// Runs the demo.
    /// </summary>
    private async Task RunAsync()
    {
        // Setup
        var logger = new TimedConsoleLogger();

        // Create the script manager
        logger.Log("Creating script manager");
        var scriptManager = await ScriptManager.CreateAsync();

        scriptManager = await DemoCompletion(
            logger,
            scriptManager,
            "System.Console.");

        scriptManager = await DemoCompletion(
            logger,
            scriptManager,
            "System.Console.W");

        scriptManager = await DemoCompletion(
            logger,
            scriptManager,
            "System.Console.Wr");

        scriptManager = await DemoCompletion(
            logger,
            scriptManager,
            "System.Console.Write");

        scriptManager = await DemoCompletion(
            logger,
            scriptManager,
            "System.Console.WriteLi");

        scriptManager = await DemoCompletion(
            logger,
            scriptManager,
            "System.Console.WriteLine");

        scriptManager = await DemoCompletion(
            logger,
            scriptManager,
            "int Fibonacci(int n) => 0; Fibo");

        scriptManager = await DemoCompletion(
            logger,
            scriptManager,
            "System.Conso");

        scriptManager = await DemoCompletion(
            logger,
            scriptManager,
            "System.Cons     int x = 10;",
            cursorPosition: 11);
    }


    /// <summary>
    /// Demonstrates the completion suggestions, using the specified script and cursor 
    /// at the end of the script.
    /// </summary>
    private static async Task<ScriptManager> DemoCompletion(
        TimedConsoleLogger logger,
        ScriptManager scriptManager,
        string script)
    {
        return await DemoCompletion(logger, scriptManager, script, script.Length);
    }


    /// <summary>
    /// Demonstrates the completion suggestions using the specified script and cursor position.
    /// </summary>
    private static async Task<ScriptManager> DemoCompletion(
        TimedConsoleLogger logger, 
        ScriptManager scriptManager, 
        string script,
        int cursorPosition)
    {
        // Apply the script
        Console.WriteLine(DashedLine);
        logger.Log($"Script: {script}");
        scriptManager = scriptManager.ApplyScript(script);

        // Get the completion suggestions
        var completions = await scriptManager.GetCompletionSuggestionsAsync(cursorPosition);
        logger.Log($"Found {completions.Count()} completions. Displaying up to the first 5");

        // Log the completions
        int count = 1;
        foreach (var completion in completions.Take(5))
        {
            logger.Log($"{count++}: {completion.DisplayText}");
        }

        return scriptManager;
    }


    /// <summary>
    /// Returns a dashed line the width of the console window.
    /// </summary>
    private static string DashedLine => new string('-', Console.WindowWidth);

}
