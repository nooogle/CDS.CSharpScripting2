using ConsoleTest.Demos;
using Spectre.Console;

namespace ConsoleTest;

class Scratch
{
    static void Main()
    {
        AnsiConsole.Write(
            new FigletText("CDS Scripting")
                .Centered()
                .Color(Color.Aqua));

        AnsiConsole.Write(
            new Rule("[bold yellow]C# Scripting 2 — Demo Console[/]")
                .RuleStyle("grey dim")
                .Centered());

        AnsiConsole.WriteLine();

        while (true)
        {
            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<MenuItem>()
                    .Title("[bold green]Select a demo[/]")
                    .PageSize(12)
                    .HighlightStyle(new Style(Color.Aqua, decoration: Decoration.Bold))
                    .UseConverter(m => m.Action is null
                        ? "[dim]Exit[/]"
                        : $"[bold]{Markup.Escape(m.Name)}[/]  [dim]{Markup.Escape(m.Description)}[/]")
                    .AddChoices(
                        new MenuItem(Demos.BasicDemo.Name, Demos.BasicDemo.Description, Demos.BasicDemo.Run),
                        new MenuItem(Demos.SharedDataDemo.Name, Demos.SharedDataDemo.Description, Demos.SharedDataDemo.Run),
                        new MenuItem(Demos.MathNetDemo.Name, Demos.MathNetDemo.Description, Demos.MathNetDemo.Run),
                        new MenuItem(Demos.OpenCvSharpDemo.Name, Demos.OpenCvSharpDemo.Description, Demos.OpenCvSharpDemo.Run),
                        new MenuItem("Code completion", "Completions sub-menu", Demos.Completions.Menu.Run),
                        new MenuItem(Demos.XMLDocDemos.Name, Demos.XMLDocDemos.Description, Demos.XMLDocDemos.Run),
                        new MenuItem(EnvInf.Name, EnvInf.Description, EnvInf.Run),
                        new MenuItem("Exit", string.Empty, null)));

            if (selected.Action is null)
                break;

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[bold yellow]{Markup.Escape(selected.Name)}[/]").RuleStyle("yellow dim"));
            AnsiConsole.WriteLine();

            selected.Action();

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to return to the menu...[/]");
            Console.ReadKey(intercept: true);
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule().RuleStyle("grey dim"));
            AnsiConsole.WriteLine();
        }
    }

    private record MenuItem(string Name, string Description, Action? Action);
}
