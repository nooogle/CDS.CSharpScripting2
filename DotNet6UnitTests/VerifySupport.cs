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
    /// Support class for the Verify library.
    /// </summary>
    public static class VerifyHelper
    {
        /// <summary>
        /// Standard settings for any test that uses Verify.
        /// </summary>
        public static VerifySettings Settings { get; }


        /// <summary>
        /// Initialize the settings for the Verify library.
        /// </summary>
        static VerifyHelper()
        {
            Settings = new();
            Settings.UseDirectory("VerifySnapshots");
        }


        /// <summary>
        /// Verify the target object using the standard settings.
        /// </summary>
        /// <param name="target">
        /// The object to verify. This can be a string, a stream, or any object that can be serialized.
        /// </param>
        /// <param name="methodName">
        /// Leave blank - the compiler will calculate this.
        /// </param>
        /// <returns>
        /// A task that can be awaited.
        /// </returns>
        public static SettingsTask Verify(object? target, [CallerMemberName] string methodName = "")
        {
            return Verifier.Verify(target, Settings).UseFileName(SimpleFileName(stackDepth: 2, methodName: methodName));
        }


        /// <summary>
        /// Verify the target object using the standard settings. The test name is used to make
        /// the verification file unique. Use this method when a test method has multiple DataRow
        /// inputs or multiple Verify calls.
        /// </summary>
        /// <param name="testName">
        /// Unique name for the test. This is used to make the verification file unique.
        /// </param>
        /// <param name="target">
        /// The object to verify. This can be a string, a stream, or any object that can be serialized.
        /// </param>
        /// <param name="methodName">
        /// Leave blank - the compiler will calculate this.
        /// </param>
        /// <returns>
        /// A task that can be awaited.
        /// </returns>
        public static SettingsTask Verify(string testName, object? target, [CallerMemberName] string methodName = "")
        {
            return Verifier.Verify(target, Settings).UseFileName(ExtendedFileName(testName, stackDepth: 2, methodName: methodName));
        }


        /// <summary>
        /// Generate a file name for a test method. This is useful only for test methods that
        /// have only a single verification and therefore won't have multiple possible verification files.
        /// The filename includeds the namespace, class name, and method name.
        /// </summary>
        /// <param name="methodName">
        /// The method name.
        /// </param>
        /// <param name="stackDepth">
        /// The stack depth to use to determine the calling class.
        /// </param>
        /// <returns>A full path and filename for the verified text file</returns>
        /// <exception cref="System.Exception">Thrown if a filename cannot be calculated</exception>
        private static string SimpleFileName(
            int stackDepth,
            string methodName)
        {
            // Get the calling class name with namespace
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(stackDepth);
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
        /// <param name="testName">
        /// The name of the test. This should be unique for each verification.
        /// </param>
        /// <param name="methodName">
        /// The method name.
        /// </param>
        /// <param name="stackDepth">
        /// The stack depth to use to determine the calling class.
        /// </param>
        /// <returns>A full path and filename for the verified text file</returns>
        /// <exception cref="System.Exception">Thrown if a filename cannot be calculated</exception>
        /// <remarks>
        /// Scenarios where this method is useful:
        /// 1. When a test method has multiple DataRow attributes.
        /// 2. When a test method has multiple Verify calls.
        /// 3. Both of the above.
        /// </remarks>
        public static string ExtendedFileName(
            string testName,
            int stackDepth,
            string methodName)
        {
            // Get the calling class name with namespace
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(stackDepth);
            var declaringType = frame?.GetMethod()?.DeclaringType;

            if (declaringType?.DeclaringType == null)
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
