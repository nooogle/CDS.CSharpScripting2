namespace ConsoleTest;

public class TestMenu
{
    private readonly List<IDemo> tests;

    public TestMenu()
    {
        tests = new List<IDemo>();
    }

    /// <summary>
    /// Adds a test to the menu.
    /// </summary>
    /// <param name="demo">The demo to add.</param>
    public void AddTest(IDemo demo)
    {
        tests.Add(demo);
    }

    /// <summary>
    /// Runs the test menu, allowing the user to select and run tests.
    /// </summary>
    public async Task RunAsync()
    {
        while (true)
        {
            Console.WriteLine($"{DashedLine}Select a test to run:");
            for (int i = 0; i < tests.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {tests[i].Name}");
            }
            Console.WriteLine("Enter 'd' to display descriptions.");
            Console.WriteLine("Enter 'q' to quit.\n");

            var input = Console.ReadLine();
            if (input?.ToLower() == "q")
            {
                break;
            }

            if (input?.ToLower() == "d")
            {
                DisplayDescriptions();
                continue;
            }

            if (int.TryParse(input, out int selection) && selection > 0 && selection <= tests.Count)
            {
                var test = tests[selection - 1];
                Console.WriteLine($"\n{DashedLine}Running test: {test.Name}\n");

                try
                {
                    await test.Run();
                    Console.WriteLine($"\n{DashedLine}Test complete.\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n{DashedLine}Test failed: {ex.Message}\n");
                }

                Console.WriteLine($"\n{DashedLine}Test complete.\n");
            }
            else
            {
                Console.WriteLine("Invalid selection. Please try again.");
            }
        }
    }

    /// <summary>
    /// Displays the descriptions of all tests.
    /// </summary>
    private void DisplayDescriptions()
    {
        Console.WriteLine($"\n{DashedLine} Test Descriptions:\n");
        for (int i = 0; i < tests.Count; i++)
        {
            Console.WriteLine($"{i + 1}: {tests[i].Name}\n{tests[i].Description}\n");
        }
    }


    /// <summary>
    /// Returns a dashed line the width of the console window.
    /// </summary>
    private static string DashedLine => new string('-', Console.WindowWidth);
}
