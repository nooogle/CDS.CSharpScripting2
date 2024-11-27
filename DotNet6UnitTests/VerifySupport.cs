using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VerifyMSTest;

[assembly: UsesVerify]

namespace VerifyTests
{
    /// <summary>
    /// Pwerforms checks on the Verify settings to ensure they are correctly configured.
    /// </summary>
    [TestClass]
    public partial class VerifyCheck
    {
        /// <summary>
        /// Runs the Verify checks which can help with customisation and configuration of the git 
        /// settings that help mamage the verification files.
        /// </summary>
        [TestMethod]
        public async Task Run()
        {
            await VerifyChecks.Run();
        }
    }


    /// <summary>
    /// Helper class to customise the settings for Verify testing.
    /// </summary>
    public static class VerifyHelper
    {
        /// <summary>
        /// Standard settings for any test that uses Verify.
        /// </summary>
        public static VerifySettings Settings { get; } = InitializeSettings();

        /// <summary>
        /// Initializes the settings for the Verify library.
        /// </summary>
        /// <returns>A configured <see cref="VerifySettings"/> instance.</returns>
        private static VerifySettings InitializeSettings()
        {
            var settings = new VerifySettings();
            settings.UseDirectory("VerifySnapshots");

            return settings;
        }
    }
}
