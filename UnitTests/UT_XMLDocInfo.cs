using AwesomeAssertions;
using CDS.CSharpScript2;
using Newtonsoft.Json;

namespace UnitTests;

[TestClass]
public partial class UT_XMLDocInfo
{
    [TestMethod]
    public async Task Should_Return_XMLDocumentation_ForSystemMathPow()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("System.Math.Pow");
        var xmlDocumentation = await new ScriptAnalyser(context).GetAPIInfoAsync(context.ScriptText.Length - 1);

        xmlDocumentation.Should().NotBeNull();
        xmlDocumentation!.MemberInfos.Should().NotBeEmpty("System.Math.Pow should have overloaded methods");
        foreach (var memberInfo in xmlDocumentation.MemberInfos)
            memberInfo.Summary.Should().NotBeNullOrWhiteSpace("each Pow overload should have summary documentation");
    }

    [TestMethod]
    public async Task Should_ReturnTypeInfo_ForConsoleType()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("Console");
        var xmlInfo = await new ScriptAnalyser(context).GetAPIInfoAsync(position: 3);

        xmlInfo.Should().NotBeNull();
        xmlInfo!.TypeInfo.Should().NotBeNull("Console should have type information");
        xmlInfo.TypeInfo.Name.Should().Be("Console");
        xmlInfo.TypeInfo.Namespace.Should().Be("System");
        xmlInfo.TypeInfo.Summary.Should().NotBeNullOrWhiteSpace("Console type should have summary documentation");
    }

    [TestMethod]
    public async Task Should_ReturnMathodInfo_ForPowMathMethod()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("System.Math.Pow(");
        var xmlInfo = await new ScriptAnalyser(context).GetAPIInfoAsync(position: 15);

        xmlInfo.Should().NotBeNull();
        xmlInfo!.TypeInfo.Should().NotBeNull();
        xmlInfo.TypeInfo.Name.Should().Be("Math");
        xmlInfo.MemberInfos.Should().NotBeEmpty("Pow should have overloaded methods");
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Name.Should().Be("Pow"));
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Summary.Should().NotBeNullOrWhiteSpace());
    }

    [TestMethod]
    public async Task Should_ReturnAllOverloadedMethods_ForConsoleWriteLine()
    {
        var script = @"Console.WriteLine";
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);
        var xmlInfo = await new ScriptAnalyser(context).GetAPIInfoAsync(script.Length - 1);

        xmlInfo.Should().NotBeNull();
        xmlInfo!.TypeInfo.Should().NotBeNull();
        xmlInfo.TypeInfo.Name.Should().Be("Console");
        xmlInfo.MemberInfos.Should().NotBeEmpty("WriteLine should have overloaded methods");
        xmlInfo.MemberInfos.Count().Should().BeGreaterThan(1, "WriteLine has many overloads");
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Name.Should().Be("WriteLine"));

        // Newer platform overloads (e.g. params ReadOnlySpan<object?>) may have no XML docs
        xmlInfo.MemberInfos.Should().Contain(m => !string.IsNullOrWhiteSpace(m.Summary),
            "at least some WriteLine overloads should have XML documentation");
    }

    [TestMethod]
    public async Task Should_ReturnAllOverloadedMethods_ForNewtonsoftMethod()
    {
        var environment =
            ScriptEnvironment
            .Default
            .WithAdditionalNamespaceType(typeof(JsonConvert))
            .WithAdditionalReferenceName(typeof(JsonConvert).Assembly!.FullName!);

        var script = @"Newtonsoft.Json.JsonConvert.SerializeObject(new object());";
        var context = await ScriptContext.CreateAsync(environment);
        context = context.ApplyScript(script);

        var executable = await new ScriptExecutor(context).CompileAsync();
        executable.CompilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        executable.CompilationOutput.Diagnostics.Should().BeEmpty("script should have no diagnostics");

        var xmlInfo = await new ScriptAnalyser(context).GetAPIInfoAsync(script.IndexOf("SerializeObject") + 2);

        xmlInfo.Should().NotBeNull();
        xmlInfo!.TypeInfo.Should().NotBeNull();
        xmlInfo.TypeInfo.Name.Should().Be("JsonConvert");
        xmlInfo.TypeInfo.Namespace.Should().Be("Newtonsoft.Json");
        xmlInfo.MemberInfos.Should().NotBeEmpty("SerializeObject should have overloaded methods");
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Name.Should().Be("SerializeObject"));
        xmlInfo.MemberInfos.Should().AllSatisfy(m =>
            m.Summary.Should().NotBeNull("XML documentation should be loaded from NuGet package"));
    }

    [TestMethod]
    public async Task Should_ReturnAllOverloadedMethods_ForOpenCvSharpClone()
    {
        var environment =
            ScriptEnvironment
            .Default
            .WithAdditionalNamespaceForType<OpenCvSharp.Mat>()
            .WithAdditionalReferenceForType<OpenCvSharp.Mat>();

        var script = @"new Mat().Clone();";
        var context = await ScriptContext.CreateAsync(environment);
        context = context.ApplyScript(script);

        var executable = await new ScriptExecutor(context).CompileAsync();
        executable.CompilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        executable.CompilationOutput.Diagnostics.Should().BeEmpty("script should have no diagnostics");

        var xmlInfo = await new ScriptAnalyser(context).GetAPIInfoAsync(script.IndexOf("Clone") + 2);

        xmlInfo.Should().NotBeNull();
        xmlInfo!.TypeInfo.Should().NotBeNull();
        xmlInfo.TypeInfo.Name.Should().Be("Mat");
        xmlInfo.TypeInfo.Namespace.Should().Be("OpenCvSharp");
        xmlInfo.MemberInfos.Should().NotBeEmpty("Clone should have method information");
        xmlInfo.MemberInfos.Should().AllSatisfy(m => m.Name.Should().Be("Clone"));
    }
}
