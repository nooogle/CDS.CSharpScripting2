using CDS.CSharpScript2;
using Newtonsoft.Json; // Added this line

namespace DotNet6UnitTests;

[TestClass]
public partial class UT_XMLDocInfo
{
    /// <summary>
    /// Check we get a XML documentation for 'System.Math.Pow'
    /// </summary>
    /// <remarks>
    /// This code was not working if a bracket was not present after the method name.
    /// </remarks>
    [TestMethod]
    public async Task Should_Return_XMLDocumentation_ForSystemMathPow()
    {
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript("System.Math.Pow");
        var xmlDocumentation = await scriptManager.GetSuggestionsAsync(scriptManager.ScriptText.Length - 1);
        await Verify(xmlDocumentation);
    }


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
    /// Check we get information about a NiuGet pacakage class method.
    /// </summary>
    /// <remarks>
    /// This relies on the Newtonsoft.Json.XML documentation file being 
    /// available at runtime in the unit test bin folder.
    /// </remarks>
    [TestMethod]
    public async Task Should_ReturnAllOverloadedMethods_ForNewtonsoftMethod()
    {
        var environment =
            ScriptEnvironment
            .Default
            .WithAdditionalNamespaceType(typeof(JsonConvert))
            .WithAdditionalReferenceName(typeof(JsonConvert).Assembly!.FullName!);

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


    /// <summary>
    /// Check we get information about a NuGet pacakage class method.
    /// </summary>
    /// <remarks>
    /// This relies on the Newtonsoft.Json.XML documentation file being 
    /// available at runtime in the unit test bin folder.
    /// </remarks>
    [TestMethod]
    public async Task Should_ReturnAllOverloadedMethods_ForOpenCvSharpClone()
    {
        var environment =
            ScriptEnvironment
            .Default
            .WithAdditionalNamespaceForType<OpenCvSharp.Mat>()
            .WithAdditionalReferenceForType<OpenCvSharp.Mat>();

        //new OpenCvSharp.Mat().Clone();

        var script = @"new Mat().Clone();";
        var scriptManager = await ScriptManager.CreateAsync(environment);
        scriptManager = scriptManager.ApplyScript(script);

        await scriptManager.CompileAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();

        var xmlInfo = await scriptManager.GetSuggestionsAsync(script.IndexOf("Clone") + 2);

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
