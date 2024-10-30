using CDS.CSScripting;
using ConsoleScratchFramework;

public static class StaticConstructorTest
{
    private static string s;

    public static string S => s;


    static StaticConstructorTest()
    {
        s = "Hello World";
    }
}


class Scratch
{
    static async Task Main(string[] args)
    {
        await Test(ScriptSamples.S5);
        await Test(ScriptSamples.S1);
        await Test(ScriptSamples.S2);
        await Test(ScriptSamples.S3);
        await Test(ScriptSamples.S4);
    }


    static async Task Test(ScriptSample scriptSample)
    {
        var code = scriptSample.Code;
        int position = scriptSample.Position;

        ScriptManager scriptManager = await ScriptManager.CreateAsync();
        scriptManager = await scriptManager.ApplyScriptAsync(code);
        SyntaxTreeVisualizer.DisplayTree(await scriptManager.GetSyntaxTreeAsync());

        await scriptManager.GetSuggestionsAsync(position);

        //await TestSimpleFunctinNameCompletion(scriptManager);
        //await CompletionTestWithEnum(scriptManager);
        //await SimpleFunctionArgs(scriptManager);
        //await TestGettingOutput(scriptManager);
    }


    private static async Task TestSimpleFunctinNameCompletion(ScriptManager scriptManager)
    {
        //System.Console.WriteLine(

        scriptManager = scriptManager.ApplyScript("int Fibonacci(int n) => 0; Fibo");
        var completions = await scriptManager.GetCompletionSuggestionsAsync(scriptManager.ScriptText.Length);

        foreach (var completion in completions)
        {
            Console.WriteLine(completion.DisplayText);
        }
    }

    private static async Task CompletionTestWithEnum(ScriptManager scriptManager)
    {
        scriptManager = scriptManager.ApplyScript("enum E {  Ant, Bat, Cat }; E e = E.B");
        var completions = await scriptManager.GetCompletionSuggestionsAsync(scriptManager.ScriptText.Length);

        foreach (var completion in completions)
        {
            Console.WriteLine(completion.DisplayText);
        }
    }

    private static async Task TestGettingOutput(ScriptManager scriptManager)
    {
        scriptManager = scriptManager.ApplyScript("int x = 2 * 3; return x;");
        var diagnostics = await scriptManager.GetDiagnosticsAsync();
        int result = await scriptManager.RunAsync<int>();
        Console.WriteLine($"Script returned: {result}");
    }
}
