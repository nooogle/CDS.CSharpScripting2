using CDS.CSScripting;
using ConsoleScratchFramework;
using OpenCvSharp;
using System.Reflection;

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
        CDS.CSScripting.Core.EnvironmentInfo.WriteToDebug();

        await OpenCvSharpTest();

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



    static async Task OpenCvSharpTest()
    {
        Globals globals = new Globals();

        string script = @"
            // Step 1: Create a black image, 256x256, 8U1C
            Mat image = new Mat(256, 256, MatType.CV_8UC1, Scalar.Black);

            // Step 2: Set a single white pixel in the center
            int centerX = image.Width / 2;
            int centerY = image.Height / 2;
            image.Set<byte>(centerY, centerX, 255);

            // Step 3: Perform a Gaussian blur of size 25x25 into a new image
            Cv2.GaussianBlur(image, HostMat, new Size(25, 25), 0);
image.CopyTo(HostMat);
        ";

        var env =
            Env
            .Default
            .WithAdditionalNamespaceForType<Mat>()
            .WithAdditionalReferenceForType<Mat>()
            .WithGlobalType<Globals>();

        var scriptManager = await ScriptManager.CreateAsync(env);
        scriptManager = await scriptManager.ApplyScriptAsync(script);

        var diagnostics = await scriptManager.GetDiagnosticsAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();
        await scriptManager.RunAsync(globals);

        byte value = globals.HostMat.At<byte>(128, 128);
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
