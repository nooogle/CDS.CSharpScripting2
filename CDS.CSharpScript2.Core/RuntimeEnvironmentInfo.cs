using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CDS.CSharpScript2;

/// <summary>
/// Provides information about the system and application.
/// </summary>
public static class RuntimeEnvironmentInfo
{
    /// <summary>
    /// Gets a string containing information about the system and application.
    /// </summary>
    public static string Get()
    {
        var info = new StringBuilder();

        // Application details
        info.Append("Application Information:");
        info.Append($"  Process Architecture: {RuntimeInformation.ProcessArchitecture} ({(Environment.Is64BitProcess ? "64-bit" : "32-bit")})");
        info.Append($"  Framework: {RuntimeInformation.FrameworkDescription}");
        info.AppendLine($"  CLR Version: {Environment.Version}");

        // OS details
        info.Append("Operating System Information:");
        info.Append($"  OS: {RuntimeInformation.OSDescription}");
        info.Append($"  Version: {Environment.OSVersion.VersionString} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})");
        info.AppendLine($"  OS Architecture: {RuntimeInformation.OSArchitecture}");

        // Machine details
        info.Append("Machine Information:");
        info.Append($"  Processor Count: {Environment.ProcessorCount}");
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
