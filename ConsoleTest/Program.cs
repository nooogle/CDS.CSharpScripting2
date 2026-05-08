using CDS.CLIMenus.Basic;
using ConsoleTest.Demos;

namespace ConsoleTest;

class Scratch
{
    static void Main()
    {
        new MenuBuilder("Main menu")
            .AddItem(Demos.BasicDemo.Name, Demos.BasicDemo.Description, Demos.BasicDemo.Run)
            .AddItem(Demos.SharedDataDemo.Name, Demos.SharedDataDemo.Description, Demos.SharedDataDemo.Run)
            .AddItem(Demos.MathNetDemo.Name, Demos.MathNetDemo.Description, Demos.MathNetDemo.Run)
            .AddItem(Demos.OpenCvSharpDemo.Name, Demos.OpenCvSharpDemo.Description, Demos.OpenCvSharpDemo.Run)
            .AddItem("Code completion", Demos.Completions.Menu.Run)
            .AddItem(Demos.XMLDocDemos.Name, Demos.XMLDocDemos.Description, Demos.XMLDocDemos.Run)
            .AddItem(EnvInf.Name, EnvInf.Description, EnvInf.Run)
            .Build()
            .Run();
    }
}
