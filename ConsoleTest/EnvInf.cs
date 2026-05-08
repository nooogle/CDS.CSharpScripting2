namespace ConsoleTest.Demos;

/// <summary>
/// Display environent information
/// </summary>
class EnvInf
{
    public static string Name => "Environment information";
    public static string Description => "Displays information about the current environment";

    public static void Run()
    {
        Console.WriteLine(TestUtils.RuntimeEnvironmentInfo.Get());
    }
}
