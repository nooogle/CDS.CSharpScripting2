using CDS.CSharpScriptUtils;

namespace ConsoleTest.Demos;

/// <summary>
/// Demonstrates how to get XML documentation for types, args, etc.
/// </summary>
class XMLDocDemos
{
    /// <summary>
    /// Gets the name of the demo.
    /// </summary>
    public static string Name => "XML documentation";

    /// <summary>
    /// Gets the description of the demo.
    /// </summary>
    public static string Description => "Demonstrates how to get XML documentation for types, args, etc.";

    /// <summary>
    /// Runs the demo.
    /// </summary>
    public static void Run()
    {
        var demo = new XMLDocDemos();
        demo.RunAsync().Wait();
    }

    /// <summary>
    /// Runs the demo.
    /// </summary>
    private async Task RunAsync()
    {
        // Setup
        var logger = new TimedConsoleLogger();

        // Create the script manager
        logger.Log("Creating script manager");
        var scriptManager = await ScriptManager.CreateAsync();

        scriptManager = await Demo1(logger, scriptManager);
        scriptManager = await Demo2(logger, scriptManager);
    }

    private static async Task<ScriptManager> Demo1(TimedConsoleLogger logger, ScriptManager scriptManager)
    {
        var script = "Math.Pow(";
        var cursorPosition = script.Length;
        scriptManager = await FindInfoForScript(logger, scriptManager, script, cursorPosition);
        return scriptManager;
    }

    private static async Task<ScriptManager> Demo2(TimedConsoleLogger logger, ScriptManager scriptManager)
    {
        var script = "Math.PI";
        var cursorPosition = script.Length - 1;
        scriptManager = await FindInfoForScript(logger, scriptManager, script, cursorPosition);
        return scriptManager;
    }

    private static async Task<ScriptManager> FindInfoForScript(
        TimedConsoleLogger logger, 
        ScriptManager scriptManager, 
        string script, 
        int cursorPosition)
    {
        // log the script and show a marker on the next line to show where the cursor is
        var intro = "Script: ";
        logger.Log($"{intro}{script}");
        logger.Log(new string(' ', intro.Length + cursorPosition) + "^");

        // Apply the script and get suggestions
        scriptManager = scriptManager.ApplyScript(script);
        var info = await scriptManager.GetSuggestionsAsync(cursorPosition);

        // display type info
        if (info.TypeInfo == null)
        {
            logger.Log("No type info found");
        }
        else
        {
            logger.Log("Displaying type info");
            logger.Log($"\tType: {info.TypeInfo.Name}");
            logger.Log($"\tNamespace: {info.TypeInfo.Namespace}");
            logger.Log($"\tAccessibility: {info.TypeInfo.Accessibility}");
            logger.Log($"\tBase type: {info.TypeInfo.BaseType}");
            logger.Log($"\tInterfaces: {string.Join(", ", info.TypeInfo.Interfaces)}");
            logger.Log($"\tSummary: {info.TypeInfo.Summary}");
            logger.Log($"\tRemarks: {info.TypeInfo.Remarks}");
        }

        // display member info
        if (!info.MemberInfos.Any())
        {
            logger.Log("No member info found");
        }
        else
        {
            logger.Log("Displaying member info");
            foreach (var memberInfo in info.MemberInfos)
            {
                logger.Log($"\tMember: {memberInfo.Name}");
                logger.Log($"\tReturn type: {memberInfo.ReturnType}");
                logger.Log($"\t\tSummary: {memberInfo.Summary}");
                logger.Log($"\t\tRemarks: {memberInfo.Remarks}");

                logger.Log($"\tParameters ({memberInfo.Parameters.Count()}):");
                foreach (var param in memberInfo.Parameters)
                {
                    logger.Log($"\t\t{param.Name}: {param.Type}");
                    logger.Log($"\t\tDoc: {param.Documentation}");
                }

            }
        }

        return scriptManager;
    }
}
