using CDS.CSScripting2;
using OpenCvSharp;
using System.Reflection;

namespace ConsoleTest;

public class Globals
{
    public Mat HostMat { get; set; } = new Mat();
}


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
        CDS.CSScripting2.Core.EnvironmentInfo.WriteToDebug();

        await OpenCVSharpTests.Test.Run();

        try
        {
            var test = Assembly.Load("System.Drawing");
            Console.WriteLine($"System.Drawing loaded from [{test.Location}]");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

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

        var d = await scriptManager.GetDiagnosticsAsync();

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
