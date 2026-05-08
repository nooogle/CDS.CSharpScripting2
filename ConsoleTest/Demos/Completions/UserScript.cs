using CDS.CSharpScript2;

namespace ConsoleTest.Demos.Completions;

class UserScript
{
    public static string Name => "Completion suggestions (user script)";
    public static string Description => "Demonstrates code completion suggestions with a user-defined script.";

    public static void Run()
    {
        new BuiltInDemos().RunAsync().Wait();
    }

    private async Task RunAsync()
    {
        Console.Clear();
        Console.WriteLine("Code completions demo");
        Console.WriteLine("=====================\n");

        var logger = new TimedConsoleLogger();

        logger.Log("Creating script context");
        var context = await ScriptContext.CreateAsync();

        context = await DemoCompletion(logger, context, "System.Console.");
        context = await DemoCompletion(logger, context, "System.Console.W");
        context = await DemoCompletion(logger, context, "System.Console.Wr");
        context = await DemoCompletion(logger, context, "System.Console.Write");
        context = await DemoCompletion(logger, context, "System.Console.WriteLi");
        context = await DemoCompletion(logger, context, "System.Console.WriteLine");
        context = await DemoCompletion(logger, context, "int Fibonacci(int n) => 0; Fibo");
        context = await DemoCompletion(logger, context, "System.Conso");
        context = await DemoCompletion(logger, context, "System.Cons     int x = 10;", cursorPosition: 11);

        Console.WriteLine("All done - press any key to exit");
        Console.ReadKey();
    }

    private static async Task<ScriptContext> DemoCompletion(
        TimedConsoleLogger logger,
        ScriptContext context,
        string script)
        => await DemoCompletion(logger, context, script, script.Length);

    private static async Task<ScriptContext> DemoCompletion(
        TimedConsoleLogger logger,
        ScriptContext context,
        string script,
        int cursorPosition)
    {
        Console.WriteLine(DashedLine);
        logger.Log($"Script: {script}");
        context = context.ApplyScript(script);

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(cursorPosition);
        logger.Log($"Found {completions.Count()} completions. Displaying up to the first 5");

        int count = 1;
        foreach (var completion in completions.Take(5))
            logger.Log($"{count++}: {completion.DisplayText}");

        return context;
    }

    private static string DashedLine => new string('-', Console.WindowWidth);
}
