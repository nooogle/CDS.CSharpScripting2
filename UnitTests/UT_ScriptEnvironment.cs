using AwesomeAssertions;
using CDS.CSharpScript2;
using Microsoft.CodeAnalysis;

namespace UnitTests;

/// <summary>
/// Covers the <see cref="ScriptEnvironment"/> configuration API, including the base directory that
/// both compilation paths use to resolve relative <c>#r</c> and <c>#load</c> paths.
/// </summary>
[TestClass]
public class UT_ScriptEnvironment
{
    private static string ExternalAssemblyPath
        => typeof(MathNet.Numerics.Distributions.Normal).Assembly.Location;

    [TestMethod]
    [TestCategory("use-cases")]
    public async Task WithBaseDirectory_RelativeReferenceDirective_CompilesAndRuns()
    {
        var environment = ScriptEnvironment.Default
            .WithBaseDirectory(Path.GetDirectoryName(ExternalAssemblyPath)!);

        var script = $"""
            #r "{Path.GetFileName(ExternalAssemblyPath)}"
            return new MathNet.Numerics.Distributions.Normal(0.0, 2.5).StdDev;
            """;

        using var root = await ScriptContext.CreateAsync(environment);
        var context = root.ApplyScript(script);

        var editorErrors = (await new ScriptAnalyser(context).GetDiagnosticsAsync())
            .Where(d => d.Severity == DiagnosticSeverity.Error);
        var executable = await new ScriptExecutor(context).CompileAsync<double>();
        var standardDeviation = await executable.RunAsync<double>();

        editorErrors.Should().BeEmpty("the editor should resolve the relative #r path");
        executable.HasErrors.Should().BeFalse();
        standardDeviation.Should().Be(2.5);
    }

    [TestMethod]
    [TestCategory("use-cases")]
    public async Task WithBaseDirectory_RelativeLoadDirective_CompilesAndRuns()
    {
        var directory = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "CDS.CSharpScript2.BaseDirectoryTest"));

        File.WriteAllText(
            Path.Combine(directory.FullName, "helper.csx"),
            "int Triple(int value) => value * 3;");

        var environment = ScriptEnvironment.Default.WithBaseDirectory(directory.FullName);

        var script = """
            #load "helper.csx"
            return Triple(4);
            """;

        using var root = await ScriptContext.CreateAsync(environment);
        var context = root.ApplyScript(script);

        var editorErrors = (await new ScriptAnalyser(context).GetDiagnosticsAsync())
            .Where(d => d.Severity == DiagnosticSeverity.Error);
        var executable = await new ScriptExecutor(context).CompileAsync<int>();
        var tripled = await executable.RunAsync<int>();

        editorErrors.Should().BeEmpty("the editor should resolve the relative #load path");
        tripled.Should().Be(12);
    }

    [TestMethod]
    public void WithBaseDirectory_RelativePath_ThrowsArgumentException()
    {
        var act = () => ScriptEnvironment.Default.WithBaseDirectory("scripts");

        act.Should().Throw<ArgumentException>().WithMessage("*absolute path*");
    }

    [TestMethod]
    public void WithBaseDirectory_Null_ThrowsArgumentNullException()
    {
        var act = () => ScriptEnvironment.Default.WithBaseDirectory(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// The base directory and script file path have to survive every other builder call. Each
    /// <c>With…</c> method constructs a fresh instance by hand, so a missed argument would
    /// silently drop one of them.
    /// </summary>
    [TestMethod]
    public void WithBaseDirectory_FollowedByOtherBuilderCalls_IsPreserved()
    {
        var directory = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var scriptPath = Path.Combine(directory, "MyScript.csx");

        var environment = ScriptEnvironment.Default
            .WithBaseDirectory(directory)
            .WithScriptFilePath(scriptPath)
            .WithAdditionalNamespaceName("System.Text")
            .WithAdditionalNamespaceType(typeof(System.Collections.ArrayList))
            .WithAdditionalNamespaceForType<System.Text.StringBuilder>()
            .WithAdditionalReferenceForType<System.Text.StringBuilder>()
            .WithAdditionalReferenceForType(typeof(System.Linq.Enumerable))
            .WithAdditionalReferenceName(typeof(System.Linq.Enumerable).Assembly.GetName().Name!)
            .WithGlobalType<UT_UseCases.GlobalData>()
            .WithGlobalType(typeof(UT_UseCases.GlobalData))
            .WithDrawingReferences();

        environment.BaseDirectory.Should().Be(directory);
        environment.ScriptFilePath.Should().Be(scriptPath);
    }

    [TestMethod]
    public void WithBaseDirectory_DoesNotMutateTheOriginal()
    {
        var original = ScriptEnvironment.Default;

        original.WithBaseDirectory(Path.GetTempPath());

        original.BaseDirectory.Should().BeNull("environments are immutable");
    }

    [TestMethod]
    public void WithScriptFilePath_SetsScriptFilePath()
    {
        var path = Path.Combine(Path.GetTempPath(), "MyScript.csx");

        var environment = ScriptEnvironment.Default.WithScriptFilePath(path);

        environment.ScriptFilePath.Should().Be(path);
    }

    [TestMethod]
    public void WithScriptFilePath_Null_ClearsAPreviouslySetPath()
    {
        var environment = ScriptEnvironment.Default
            .WithScriptFilePath(Path.Combine(Path.GetTempPath(), "MyScript.csx"))
            .WithScriptFilePath(null);

        environment.ScriptFilePath.Should().BeNull();
    }

    [TestMethod]
    public void WithScriptFilePath_DoesNotMutateTheOriginal()
    {
        var original = ScriptEnvironment.Default;

        original.WithScriptFilePath(Path.Combine(Path.GetTempPath(), "MyScript.csx"));

        original.ScriptFilePath.Should().BeNull("environments are immutable");
    }

    [TestMethod]
    public void WithAdditionalReferenceForType_StaticType_AddsTheAssembly()
    {
        var environment = ScriptEnvironment.Default
            .WithAdditionalReferenceForType(typeof(System.Linq.Enumerable));

        environment.References.Should().Contain(typeof(System.Linq.Enumerable).Assembly);
    }

    [TestMethod]
    public void WithAdditionalReferenceForType_Null_ThrowsArgumentNullException()
    {
        var act = () => ScriptEnvironment.Default.WithAdditionalReferenceForType(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// On .NET Framework a simple name for a strongly-named GAC assembly is rejected by
    /// <c>Assembly.Load</c>, so the environment has to fall back to the loaded assembly.
    /// </summary>
    [TestMethod]
    public void WithAdditionalReferenceName_SimpleName_AddsTheAssembly()
    {
        var linqAssembly = typeof(System.Linq.Enumerable).Assembly;

        var environment = ScriptEnvironment.Default
            .WithAdditionalReferenceName(linqAssembly.GetName().Name!);

        environment.References.Should().Contain(linqAssembly);
    }

    [TestMethod]
    public void WithAdditionalReferenceName_FullDisplayName_AddsTheAssembly()
    {
        var linqAssembly = typeof(System.Linq.Enumerable).Assembly;

        var environment = ScriptEnvironment.Default
            .WithAdditionalReferenceName(linqAssembly.FullName!);

        environment.References.Should().Contain(linqAssembly);
    }

    [TestMethod]
    public void WithAdditionalReferenceName_UnknownAssembly_ThrowsArgumentException()
    {
        var act = () => ScriptEnvironment.Default.WithAdditionalReferenceName("No.Such.Assembly");

        act.Should().Throw<ArgumentException>().WithMessage("*No.Such.Assembly*");
    }

    [TestMethod]
    public void WithAdditionalReferenceName_Empty_ThrowsArgumentException()
    {
        var act = () => ScriptEnvironment.Default.WithAdditionalReferenceName("   ");

        act.Should().Throw<ArgumentException>();
    }
}
