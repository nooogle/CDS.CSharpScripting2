using CDS.CLIMenus.Basic;

namespace ConsoleTest;

class Scratch
{
    static void Main()
    {
        //CompletionsRnD.Run().Wait();

        new MenuBuilder("Main menu")
            .AddItem(Demos.BasicDemo.Name, Demos.BasicDemo.Description, Demos.BasicDemo.Run)
            .AddItem(Demos.SharedDataDemo.Name, Demos.SharedDataDemo.Description, Demos.SharedDataDemo.Run)
            .AddItem(Demos.MathNetDemo.Name, Demos.MathNetDemo.Description, Demos.MathNetDemo.Run)
            .AddItem(Demos.OpenCvSharpDemo.Name, Demos.OpenCvSharpDemo.Description, Demos.OpenCvSharpDemo.Run)
            .AddItem("Code completion", Demos.Completions.Menu.Run)
            .AddItem(Demos.XMLDocDemos.Name, Demos.XMLDocDemos.Description, Demos.XMLDocDemos.Run)
            .Build()
            .Run();

        //try
        //{
        //    var test = Assembly.Load("System.Drawing");
        //    Console.WriteLine($"System.Drawing loaded from [{test.Location}]");
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine(ex.Message);
        //}

        //await Test(ScriptSamples.S5);
        //await Test(ScriptSamples.S1);
        //await Test(ScriptSamples.S2);
        //await Test(ScriptSamples.S3);
        //await Test(ScriptSamples.S4);
    }



    //static async Task Test(ScriptSample scriptSample)
    //{
    //    var code = scriptSample.Code;
    //    int position = scriptSample.Position;

    //    ScriptManager scriptManager = await ScriptManager.CreateAsync();
    //    scriptManager = await scriptManager.ApplyScriptAsync(code);
    //    SyntaxTreeVisualizer.DisplayTree(await scriptManager.GetSyntaxTreeAsync());

    //    var d = await scriptManager.GetDiagnosticsAsync();

    //    await scriptManager.GetSuggestionsAsync(position);

    //    //await TestSimpleFunctinNameCompletion(scriptManager);
    //    //await CompletionTestWithEnum(scriptManager);
    //    //await SimpleFunctionArgs(scriptManager);
    //    //await TestGettingOutput(scriptManager);
    //}


    //private static async Task TestSimpleFunctinNameCompletion(ScriptManager scriptManager)
    //{
    //    //System.Console.WriteLine(

    //    scriptManager = scriptManager.ApplyScript("int Fibonacci(int n) => 0; Fibo");
    //    var completions = await scriptManager.GetCompletionSuggestionsAsync(scriptManager.ScriptText.Length);

    //    foreach (var completion in completions)
    //    {
    //        Console.WriteLine(completion.DisplayText);
    //    }
    //}

    //private static async Task CompletionTestWithEnum(ScriptManager scriptManager)
    //{
    //    scriptManager = scriptManager.ApplyScript("enum E {  Ant, Bat, Cat }; E e = E.B");
    //    var completions = await scriptManager.GetCompletionSuggestionsAsync(scriptManager.ScriptText.Length);

    //    foreach (var completion in completions)
    //    {
    //        Console.WriteLine(completion.DisplayText);
    //    }
    //}

    //private static async Task TestGettingOutput(ScriptManager scriptManager)
    //{
    //    scriptManager = scriptManager.ApplyScript("int x = 2 * 3; return x;");
    //    var diagnostics = await scriptManager.GetDiagnosticsAsync();
    //    int result = await scriptManager.RunAsync<int>();
    //    Console.WriteLine($"Script returned: {result}");
    //}
}
