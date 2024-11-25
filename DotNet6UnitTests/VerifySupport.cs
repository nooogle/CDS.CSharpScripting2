using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

[assembly: UsesVerify]

namespace DotNet6UnitTests
{
    /// <summary>
    /// Support class for the Verify library.
    /// </summary>
    [TestClass]
    public partial class VerifySupport
    {
        /// <summary>
        /// Standard settings for any test that uses Verify.
        /// </summary>
        public static VerifySettings Settings { get; }


        /// <summary>
        /// Initialize the settings for the Verify library.
        /// </summary>
        static VerifySupport()
        {
            Settings = new();
            Settings.UseDirectory("VerifySnapshots");
        }


        /// <summary>
        /// Runs the Verify checks which can help with customisation and configuration of the git 
        /// settings that help mamage the verification files.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Run()
        {
            await VerifyChecks.Run();
        }



        /// <summary>
        /// Generate a file name for a test method. This is useful only for test methods that
        /// have only a single verification and therefore won't have multiple possible verification files.
        /// The filename includeds the namespace, class name, and method name.
        /// </summary>
        /// <param name="methodName">Leave blank - the compiler will calculate this</param>
        /// <returns>A full path and filename for the verified text file</returns>
        /// <exception cref="System.Exception">Thrown if a filename cannot be calculated</exception>
        public static string SimpleFileName(
                [CallerMemberName] string methodName = "")
        {
            // Get the calling class name with namespace
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(1); // Get the immediate caller
            var declaringType = frame?.GetMethod()?.DeclaringType;

            if (declaringType?.DeclaringType == null)
            {
                throw new System.Exception("Failed to determine the declaring type of the caller!");
            }

            string fileName = $"{declaringType.DeclaringType.Namespace}.{declaringType.DeclaringType.Name}.{methodName}";
            fileName = SanitizeFileName(fileName);

            return fileName;
        }


        /// <summary>
        /// Generate a file name for a test method. This is useful only for test methods that
        /// may result in multiple tests being verified in a single test method.
        /// The filename includeds the namespace, class name, method name, and test name.
        /// </summary>
        /// <param name="testName">A unique name for the specific test</param>
        /// <param name="methodName">Leave blank - the compiler will calculate this</param>
        /// <returns>A full path and filename for the verified text file</returns>
        /// <exception cref="System.Exception">Thrown if a filename cannot be calculated</exception>
        /// <remarks>
        /// There are two scenarios where this method is useful:
        /// 1. When a test method has multiple DataRow attributes.
        /// 2. When a test method has multiple Verify calls.
        /// </remarks>
        public static string ExtendedFileName(
                string testName,
                [CallerMemberName] string methodName = "")
        {
            // Get the calling class name with namespace
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(1); // Get the immediate caller
            var declaringType = frame?.GetMethod()?.DeclaringType;

            if(declaringType?.DeclaringType == null)
            {
                throw new System.Exception("Failed to determine the declaring type of the caller!");
            }

            string fileName = $"{declaringType.DeclaringType.Namespace}.{declaringType.DeclaringType.Name}.{methodName}.{testName}";
            fileName = SanitizeFileName(fileName);

            return fileName;
        }


        /// <summary>
        /// Sanitize a string to be used as a filename.
        /// </summary>
        private static string SanitizeFileName(string input)
        {
            // Replace invalid characters with underscores
            return Regex.Replace(input, @"[<>:""/\\|?*\s]+", "_");
        }
    }
}
