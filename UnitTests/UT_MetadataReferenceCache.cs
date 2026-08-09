using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Editors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnitTests;

/// <summary>
/// Covers the process-wide cache behind <c>#r</c> resolution.
/// </summary>
/// <remarks>
/// Resolved references are cached so Roslyn can reuse a compilation's binding between
/// keystrokes. The risk that buys is staleness: a user who rebuilds a referenced assembly
/// must see the new build, and must not find the file locked in the meantime. Fixtures are
/// emitted here rather than borrowed from any other project.
/// </remarks>
[TestClass]
[TestCategory("reference directives")]
public class UT_MetadataReferenceCache
{
    private string _directory = string.Empty;

    [TestInitialize]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "cds_refcache_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void TearDown()
    {
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    /// <summary>Emits a small assembly to <paramref name="path"/>, overwriting any existing file.</summary>
    private static void Emit(string path, string source)
    {
        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(path),
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        var result = compilation.Emit(stream);

        result.Success.Should().BeTrue(
            "the fixture assembly must compile: {0}",
            string.Join("; ", result.Diagnostics.Select(d => d.ToString())));
    }

    private static async Task<int> ErrorCountAsync(EditorManager manager, string script)
    {
        await manager.ApplyScript(script);
        return manager.LastDiagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
    }

    [TestMethod]
    public async Task ReferencedAssembly_RebuiltWithNewMember_IsPickedUp()
    {
        var dll = Path.Combine(_directory, "Fixture.dll");
        Emit(dll, "public static class Api { public static int Alpha() => 1; }");

        var script = $"#r \"{dll.Replace("\\", "\\\\")}\"\nvar a = Api.Alpha();\nvar b = Api.Beta();\n";

        using var manager = new EditorManager(ScriptEnvironment.Default);

        var before = await ErrorCountAsync(manager, script);
        before.Should().BeGreaterThan(0, "Beta does not exist in the first build");

        // A distinct write time is what invalidates the cache entry.
        await Task.Delay(1100);
        Emit(dll, "public static class Api { public static int Alpha() => 1; public static int Beta() => 2; }");

        var after = await ErrorCountAsync(manager, script + "\n");
        after.Should().Be(0, "the rebuilt assembly should be resolved afresh");
    }

    [TestMethod]
    public async Task ReferencedAssembly_Unchanged_StillResolves()
    {
        var dll = Path.Combine(_directory, "Stable.dll");
        Emit(dll, "public static class Api { public static int Alpha() => 1; }");

        var script = $"#r \"{dll.Replace("\\", "\\\\")}\"\nvar a = Api.Alpha();\n";

        using var manager = new EditorManager(ScriptEnvironment.Default);

        // Repeated passes exercise the cache-hit path.
        for (int i = 0; i < 3; i++)
        {
            var errors = await ErrorCountAsync(manager, script + $"\nvar pad{i} = {i};\n");
            errors.Should().Be(0, "a cached reference must keep resolving correctly on pass {0}", i);
        }
    }

    [TestMethod]
    public async Task ReferencedAssembly_HeldByTheCache_IsNotLockedAgainstRebuild()
    {
        // A locked DLL would stop the user rebuilding the library their script references,
        // which would be a worse problem than the one the cache solves.
        var dll = Path.Combine(_directory, "Unlocked.dll");
        Emit(dll, "public static class Api { public static int Alpha() => 1; }");

        var script = $"#r \"{dll.Replace("\\", "\\\\")}\"\nvar a = Api.Alpha();\n";

        using var manager = new EditorManager(ScriptEnvironment.Default);
        await manager.ApplyScript(script);

        var overwrite = () => Emit(dll, "public static class Api { public static int Alpha() => 2; }");
        overwrite.Should().NotThrow("holding a cached reference must not lock the file");

        var delete = () => File.Delete(dll);
        delete.Should().NotThrow("nor prevent it being deleted");
    }
}
