using Spectre.Console;

namespace ConsoleTest.Demos.Completions;

class Menu
{
    public static void Run()
    {
        while (true)
        {
            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<MenuItem>()
                    .Title("[bold green]Select a completion demo[/]")
                    .HighlightStyle(new Style(Color.Aqua, decoration: Decoration.Bold))
                    .UseConverter(m => m.Action is null ? "[dim]Back[/]" : $"[bold]{Markup.Escape(m.Name)}[/]")
                    .AddChoices(
                        new MenuItem("Built-in demos", BuiltInDemos.Run),
                        new MenuItem("User entered script", UserScript.Run),
                        new MenuItem("Back", null)));

            if (selected.Action is null)
                break;

            AnsiConsole.WriteLine();
            selected.Action();
            AnsiConsole.WriteLine();
        }
    }

    private record MenuItem(string Name, Action? Action);
}
