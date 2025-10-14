using AwesomeAssertions;
using CDS.CSharpScript2;
using Newtonsoft.Json;

namespace UnitTests;

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

        // Verify we got API info
        xmlDocumentation.Should().NotBeNull();
        
        // Verify we have member information (Pow method overloads)
        xmlDocumentation.MemberInfos.Should().NotBeEmpty("System.Math.Pow should have overloaded methods");
        
        // Verify each overload has documentation
        foreach (var memberInfo in xmlDocumentation.MemberInfos)
        {
            memberInfo.Summary.Should().NotBeNullOrWhiteSpace("each Pow overload should have summary documentation");
        }
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

        // Verify we got API info
        xmlInfo.Should().NotBeNull();
        
        // Verify we have type information
        xmlInfo.TypeInfo.Should().NotBeNull("Console should have type information");
        xmlInfo.TypeInfo.Name.Should().Be("Console");
        xmlInfo.TypeInfo.Namespace.Should().Be("System");
        
        // Console type should have XML documentation
        xmlInfo.TypeInfo.Summary.Should().NotBeNullOrWhiteSpace("Console type should have summary documentation");
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

        // Verify we got API info
        xmlInfo.Should().NotBeNull();
        
        // Verify we have type information for Math
        xmlInfo.TypeInfo.Should().NotBeNull();
        xmlInfo.TypeInfo.Name.Should().Be("Math");
        
        // Verify we have member information for Pow overloads
        xmlInfo.MemberInfos.Should().NotBeEmpty("Pow should have overloaded methods");
        
        // All overloads should be named "Pow"
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Name.Should().Be("Pow"));
        
        // All overloads should have documentation
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Summary.Should().NotBeNullOrWhiteSpace());
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

        // Verify we got API info
        xmlInfo.Should().NotBeNull();
        
        // Verify we have type information for Console
        xmlInfo.TypeInfo.Should().NotBeNull();
        xmlInfo.TypeInfo.Name.Should().Be("Console");
        
        // Verify we have multiple WriteLine overloads
        xmlInfo.MemberInfos.Should().NotBeEmpty("WriteLine should have overloaded methods");
        xmlInfo.MemberInfos.Count().Should().BeGreaterThan(1, "WriteLine has many overloads");
        
        // All overloads should be named "WriteLine"
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Name.Should().Be("WriteLine"));
        
        // All overloads should have documentation
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Summary.Should().NotBeNullOrWhiteSpace());
    }

    /// <summary>
    /// Check we get information about a NuGet package class method.
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

        // Verify compilation succeeded
        compilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        compilationOutput.Diagnostics.Should().BeEmpty("script should have no diagnostics");

        var xmlInfo = await scriptManager.GetSuggestionsAsync(script.IndexOf("SerializeObject") + 2);

        // Verify we got API info
        xmlInfo.Should().NotBeNull();
        
        // Verify we have type information for JsonConvert
        xmlInfo.TypeInfo.Should().NotBeNull();
        xmlInfo.TypeInfo.Name.Should().Be("JsonConvert");
        xmlInfo.TypeInfo.Namespace.Should().Be("Newtonsoft.Json");
        
        // Verify we have member information for SerializeObject overloads
        xmlInfo.MemberInfos.Should().NotBeEmpty("SerializeObject should have overloaded methods");
        
        // All overloads should be named "SerializeObject"
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Name.Should().Be("SerializeObject"));
        
        // With XML documentation available, overloads should have documentation
        // Note: This assertion might need adjustment based on whether XML docs are actually available
        xmlInfo.MemberInfos.Should().AllSatisfy(m => 
            m.Summary.Should().NotBeNull("XML documentation should be loaded from NuGet package"));
    }

    /// <summary>
    /// Check we get information about a NuGet package class method.
    /// </summary>
    /// <remarks>
    /// This relies on the OpenCvSharp4.Windows XML documentation file being 
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

        var script = @"new Mat().Clone();";
        var scriptManager = await ScriptManager.CreateAsync(environment);
        scriptManager = scriptManager.ApplyScript(script);

        await scriptManager.CompileAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();

        // Verify compilation succeeded
        compilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        compilationOutput.Diagnostics.Should().BeEmpty("script should have no diagnostics");

        var xmlInfo = await scriptManager.GetSuggestionsAsync(script.IndexOf("Clone") + 2);

        // Verify we got API info
        xmlInfo.Should().NotBeNull();
        
        // Verify we have type information for Mat
        xmlInfo.TypeInfo.Should().NotBeNull();
        xmlInfo.TypeInfo.Name.Should().Be("Mat");
        xmlInfo.TypeInfo.Namespace.Should().Be("OpenCvSharp");
        
        // Verify we have member information for Clone method
        xmlInfo.MemberInfos.Should().NotBeEmpty("Clone should have method information");
        
        // Verify the method is named "Clone"
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Name.Should().Be("Clone"));
    }
}
