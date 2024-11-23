using CDS.CSScripting;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace DotNet6UnitTests
{
    [TestClass]
    public class UT_Compilation
    {
        /// <summary>
        /// Test that references are resolved during compilation.
        /// </summary>
        [TestMethod]
        public async Task NoErrors_GeneratedDurationCompilation_OfValidScriptAndEnv()
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

            compilationOutput.Should().NotBeNull();
            compilationOutput.ErrorCount.Should().Be(0);
        }
    }
}
