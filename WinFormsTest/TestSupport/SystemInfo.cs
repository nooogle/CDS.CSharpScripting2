using System.Runtime.InteropServices;

namespace WinFormsTest.TestSupport;


/// <summary>
/// Provides information about the system and application.
/// </summary>
public static class SystemInfo
{
    /// <summary>
    /// Gets a string containing information about the system and application.
    /// </summary>
    public static string Get()
    {
        string appName = Application.ProductName!;
        string appVersion = Application.ProductVersion.Split('+')[0]; // Remove hash if present

        string appBitDepth = Environment.Is64BitProcess ? "64-bit" : "32-bit";
        string appArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
        string appFramework = RuntimeInformation.FrameworkDescription;

        string osVersion = Environment.OSVersion.VersionString;
        string osBitDepth = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        string osArchitecture = RuntimeInformation.OSArchitecture.ToString();

        return
            $"Application: {appName} [{appVersion}] " +
            $"running as {appBitDepth} {appArchitecture} " +
            $"using {appFramework} " +
            $"on {osVersion} {osBitDepth} and {osArchitecture} processor";
    }
}

