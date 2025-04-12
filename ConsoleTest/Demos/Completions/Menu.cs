using CDS.CLIMenus.Basic;

namespace ConsoleTest.Demos.Completions;

class Menu
{
    public static void Run()
    {
        new MenuBuilder("Completions")
            .AddItem("Built-in demos", BuiltInDemos.Run)
            .AddItem("User entered script", UserScript.Run)
            .Build()
            .Run();
    }
}
