using CDS.CSScripting;
using OpenCvSharp;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;


class Scratch
{
    static async Task Main(string[] args)
    {
        await CSTest();

        string script = @"
            Mat mat = Cv2.ImRead(@""C:\Users\jon\Downloads\OCR\4.jpg"", ImreadModes.Grayscale);
            Mat blurred = new Mat();
            Cv2.GaussianBlur(mat, blurred, new Size(5, 5), 0);
            ";

        script = "";
        //script = @"using OpenCvSharp; Mat mat = null;";
        //script = @"Mat mat = null;";

        var env =
            Env
            .Default
            .WithAdditionalNamespaceForType<Mat>();

        env = env.WithAdditionalReferenceForType<Mat>();

        var scriptManager = await ScriptManager.CreateAsync(env);
        scriptManager = await scriptManager.ApplyScriptAsync(script);

        var diagnostics = await scriptManager.GetDiagnosticsAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();
    }


    static async Task CSTest()
    {
        string code = @"
using OpenCvSharp;
public int Add(int x, int y) => x + y;";

        // Add necessary references and imports
        var options = ScriptOptions.Default
            .AddReferences(typeof(OpenCvSharp.Mat).Assembly) // Add OpenCvSharp assembly
            .AddImports("System", "OpenCvSharp");

        var script = CSharpScript.Create<int>(code, options);

        // Compile the script once
        var compiledScriptDiagnostics = script.Compile();
        if (compiledScriptDiagnostics.Length == 0) // No diagnostics
        {
            // Run the script multiple times without recompiling
            var result1 = await script.RunAsync();
            var result2 = await script.RunAsync();
        }
    }
}
