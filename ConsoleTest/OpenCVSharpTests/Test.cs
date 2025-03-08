using CDS.CSScripting2;
using OpenCvSharp;

namespace ConsoleTest.OpenCVSharpTests;


class Test
{
    public static async Task Run()
    {
        Globals globals = new Globals();

        string script = @"
            // Step 1: Create a black image, 256x256, 8U1C
            GlobalMat = new Mat(256, 256, MatType.CV_8UC1, Scalar.Black);

            // Step 2: Set a single white pixel in the center
            int centerX = GlobalMat.Width / 2;
            int centerY = GlobalMat.Height / 2;
            GlobalMat.Set<byte>(centerY, centerX, 255);
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

        byte value1 = globals.GlobalMat.At<byte>(128, 128);
        byte value2 = globals.GlobalMat.At<byte>(129, 128);

    }

}
