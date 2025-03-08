    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;

namespace CDS.CSScripting2.Core
{
    /// <summary>
    /// Provides information about the system and application.
    /// </summary>
    public static class EnvironmentInfo
    {
        /// <summary>
        /// Gets a string containing information about the system and application.
        /// </summary>
        public static string Get()
        {
            var info = new StringBuilder();

            // Application details
            info.AppendLine("Application Information:");
            info.AppendLine($"  Process Architecture: {RuntimeInformation.ProcessArchitecture} ({(Environment.Is64BitProcess ? "64-bit" : "32-bit")})");
            info.AppendLine($"  Framework: {RuntimeInformation.FrameworkDescription}");
            info.AppendLine($"  CLR Version: {Environment.Version}");

            // OS details
            info.AppendLine("Operating System Information:");
            info.AppendLine($"  OS: {RuntimeInformation.OSDescription}");
            info.AppendLine($"  Version: {Environment.OSVersion.VersionString} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})");
            info.AppendLine($"  OS Architecture: {RuntimeInformation.OSArchitecture}");

            // Machine details
            info.AppendLine("Machine Information:");
            info.AppendLine($"  Processor Count: {Environment.ProcessorCount}");
            info.AppendLine($"  Machine Name: {Environment.MachineName}");

            return info.ToString();
        }

        /// <summary>
        /// Writes the environment information to the debug output.
        /// </summary>
        public static void WriteToDebug()
        {
            Debug.WriteLine(Get());
        }
    }
}
