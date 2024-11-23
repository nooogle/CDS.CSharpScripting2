using CDS.CSScripting;
using OpenCvSharp;
using System.Threading.Tasks;

public class Globals
{
    public Mat HostMat { get; set; } = new Mat();
}


class Scratch
{
    static async Task Main(string[] args)
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
}

