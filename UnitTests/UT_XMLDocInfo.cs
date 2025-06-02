using CDS.CSharpScript2;

namespace DotNet6UnitTests;

[TestClass]
public partial class UT_XMLDocInfo
{
    /// <summary>
    /// Check we get some type information when hovering over 'Console'
    /// </summary>
    [TestMethod]
    public async Task Should_ReturnTypeInfo_ForConsoleType()
    {
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript("Console");
        var xmlInfo = await scriptManager.GetSuggestionsAsync(position: 3);

        await Verify(xmlInfo);
    }


    /// <summary>
    /// Check we get some type information when hovering over Pow (from Math.Pow)
    /// </summary>
    [TestMethod]
    public async Task Should_ReturnMathodInfo_ForPowMathMethod()
    {
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript("System.Math.Pow(");
        var xmlInfo = await scriptManager.GetSuggestionsAsync(position: 15);

        await Verify(xmlInfo);
    }

    /// <summary>
    /// Check we get information about all the overloads of Console.WriteLine
    /// </summary>
    [TestMethod]
    public async Task Should_ReturnAllOverloadedMethods_ForConsoleWriteLine()
    {
        var script = @"Console.WriteLine";
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript(script);
        var xmlInfo = await scriptManager.GetSuggestionsAsync(script.Length - 1);

        await Verify(xmlInfo);
    }

    /// <summary>
    /// Check we get information about all the overloads of Console.WriteLine
    /// </summary>
    [TestMethod]
    public async Task Should_ReturnAllOverloadedMethods_ForNewtonsoftMethod()
    {
        var environment =
            ScriptEnvironment
            .Default
            .WithAdditionalNamespaceType(typeof(Newtonsoft.Json.JsonConvert))
            .WithAdditionalReferenceName("Newtonsoft.Json");

        var script = @"Newtonsoft.Json.JsonConvert.SerializeObject(new object());";
        var scriptManager = await ScriptManager.CreateAsync(environment);
        scriptManager = scriptManager.ApplyScript(script);

        await scriptManager.CompileAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();

        var xmlInfo = await scriptManager.GetSuggestionsAsync(script.IndexOf("SerializeObject") + 2);

        var assertionInfo = new
        {
            compilationOutput,
            xmlInfo,
        };

        await Verify(assertionInfo);
    }



    // TODO this doesn't work!

    //    /// <summary>
    //    /// Check we get information about all the overloads of Console.WriteLine
    //    /// </summary>
    //    [TestMethod]
    //    public async Task Should_ReturnXMLInfo_ForScriptMethod()
    //    {

    //        var script =
    //@"    /// <summary>
    //    /// Returns the sum of two integers.
    //    /// </summary>
    //    /// <param name=""a"">The first integer</param>
    //    /// <param name=""b"">The second integer</param>
    //    /// <returns>The sum of the two integers</returns>
    //    int Add(int a, int b) => a + b;
    //";

    //        var scriptManager = await ScriptManager.CreateAsync();
    //        scriptManager = scriptManager.ApplyScript(script);
    //        var xmlInfo = await scriptManager.GetSuggestionsAsync(script.IndexOf("Add") + 2);

    //        await Verify(xmlInfo);
    //    }
}
