using CDS.CSharpScript2;

namespace ConsoleTest.Demos.Completions;

class UserScript
{
    public static string Name => "Completion suggestions (user script)";
    public static string Description => "Demonstrates code completion suggestions with a user-defined script.";

    public static void Run()
    {
       // new BuiltInDemos().RunAsync().Wait();
    }


    private static string DashedLine => new string('-', Console.WindowWidth);
}
