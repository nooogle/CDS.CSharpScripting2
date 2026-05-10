using CDS.CSharpScript2;

namespace ConsoleTest.Demos;

class XMLDocDemos
{
    public static string Name => "XML documentation";
    public static string Description => "Demonstrates how to get XML documentation for types, args, etc.";

    public static void Run()
    {
        new XMLDocDemos().RunAsync().Wait();
    }

    private async Task RunAsync()
    {
        var logger = new TimedConsoleLogger();

        logger.Log("Creating script context");
        var context = await ScriptContext.CreateAsync();

        context = await Demo1(logger, context);
        context = await Demo2(logger, context);
    }

    private static async Task<ScriptContext> Demo1(TimedConsoleLogger logger, ScriptContext context)
    {
        var script = "Math.Pow(";
        return await FindInfoForScript(logger, context, script, cursorPosition: script.Length - 1);
    }

    private static async Task<ScriptContext> Demo2(TimedConsoleLogger logger, ScriptContext context)
    {
        var script = "Console.WriteLine";
        return await FindInfoForScript(logger, context, script, cursorPosition: script.Length - 1);
    }

    private static async Task<ScriptContext> FindInfoForScript(
        TimedConsoleLogger logger,
        ScriptContext context,
        string script,
        int cursorPosition)
    {
        var intro = "Script: ";
        logger.Log($"{intro}{script}");
        logger.Log(new string(' ', intro.Length + cursorPosition) + "^");

        context = context.ApplyScript(script);
        var info = await new ScriptAnalyser(context).GetAPIInfoAsync(cursorPosition);

        if (info?.TypeInfo == null)
        {
            logger.Log("No type info found");
        }
        else
        {
            logger.Log($"\tType: {info.TypeInfo.Name}");
            logger.Log($"\tNamespace: {info.TypeInfo.Namespace}");
            logger.Log($"\tSummary: {info.TypeInfo.Summary}");
        }

        var memberInfos = info?.MemberInfos ?? [];

        if (!memberInfos.Any())
        {
            logger.Log("No member info found");
        }
        else
        {
            foreach (var memberInfo in memberInfos)
            {
                logger.Log($"\tMember: {memberInfo.Name}");
                logger.Log($"\tReturn type: {memberInfo.ReturnType}");
                logger.Log($"\t\tSummary: {memberInfo.Summary}");
                foreach (var param in memberInfo.Parameters)
                {
                    logger.Log($"\t\t{param.Name}: {param.Type}");
                    logger.Log($"\t\tDoc: {param.Documentation}");
                }
            }
        }

        return context;
    }
}
