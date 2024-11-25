using CDS.CSScripting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCvSharp;
using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;


namespace DotNet6UnitTests
{
    [TestClass]
    public partial class UT_Compilation
    {
    
        /// <summary>
        /// Test that references are resolved during compilation.
        /// </summary>
        /// <remarks>
        /// This tests that a bug fix has been applied to the compilation process.
        /// Originally, the references were not being resolved correctly, causing
        /// the script compilation to fail.
        /// </remarks>
        [TestMethod]
        public async Task NoErrorsGenerated_DurationCompilation_OfValidScriptAndEnv()
        {
            var script = "";

            var env =
                Env
                .Default
                .WithAdditionalNamespaceForType<OpenCvSharp.Mat>()
                .WithAdditionalReferenceForType<OpenCvSharp.Mat>();

            var scriptManager = await ScriptManager.CreateAsync(env);
            scriptManager = await scriptManager.ApplyScriptAsync(script);
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();

            // Verify
            await
                Verifier
                .Verify(compilationOutput, VerifySupport.Settings)
                .UseFileName(VerifySupport.SimpleFileName());
        }


        /// <summary>
        /// Globals class used to store global variables for the script.
        /// </summary>
        public class Globals
        {
            public int ImageSize { get; set; } = 256;
            public Mat HostMat { get; set; } = new Mat();
        }


        [TestMethod]
        public async Task HostImage_ModifiedByScript_ContainsNewPixels()
        {
            Globals globals = new Globals();

            string script = @"
Mat image = new Mat(ImageSize, ImageSize, MatType.CV_8UC1, Scalar.Black);

// Set a single white pixel in the center
int centerX = image.Width / 2;
int centerY = image.Height / 2;
image.Set<byte>(centerY, centerX, 255);

// Perform a Gaussian blur of size 25x25 into the host image
Cv2.GaussianBlur(image, HostMat, new Size(25, 25), 0);
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

            byte value = globals.HostMat.At<byte>(globals.ImageSize / 2, globals.ImageSize / 2);

            // Verify
            await
                Verifier
                .Verify(value, VerifySupport.Settings)
                .UseFileName(VerifySupport.SimpleFileName());
        }
    }
}
