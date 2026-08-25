using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Text;

namespace CDS.CSharpScript2;

/// <summary>
/// Provides methods for compiling C# scripts into <see cref="CompiledScript"/> instances.
/// </summary>
internal static class ScriptCompiler
{
    /// <summary>
    /// Compile a C# script that returns a specific type.
    /// </summary>
    /// <param name="script">Script text to compile.</param>
    /// <param name="environment">
    /// Configuration supplying the namespace imports, assembly references, globals type, and the
    /// resolvers used for <c>#r</c> and <c>#load</c> directives.
    /// </param>
    /// <typeparam name="TReturn">The type of object returned from the script.</typeparam>
    /// <returns>A compiled script.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="environment"/> is <see langword="null"/>.</exception>
    public static CompiledScript Compile<TReturn>(string script, ScriptEnvironment environment)
    {
        if (environment == null)
        {
            throw new ArgumentNullException(nameof(environment));
        }

        // Every setting comes from the environment, which is also what ScriptContext configures the
        // editor's workspace from. That shared source is what keeps the two paths in agreement.
        //
        // Debug info is unconditional: without it, an exception thrown from the script reports an
        // unlocatable "Submission#0" frame with no file or line, which is close to useless for a
        // host trying to tell a user where their script failed. WithFileEncoding is mandatory
        // alongside WithEmitDebugInformation — Roslyn silently emits no debug info for source text
        // with no encoding, turning this into a missing line number rather than a compile failure.
        var scriptOptions = ScriptOptions.Default
            .WithImports(environment.NamespaceNames)
            .AddReferences(environment.References)
            .WithMetadataResolver(environment.MetadataResolver)
            .WithSourceResolver(environment.SourceResolver)
            .WithEmitDebugInformation(true)
            .WithFilePath(environment.ScriptFilePath ?? "script.csx")
            .WithFileEncoding(Encoding.UTF8);

        // ScriptContext.CreateCore adds the globals type's own assembly to the editor's references
        // in addition to environment.References; mirrored here so a script naming another type from
        // that assembly compiles the same way in both paths. Without it, this path alone reports
        // CS0234/CS0246 on such a type with no corresponding editor squiggle.
        if (environment.GlobalType != null)
        {
            scriptOptions = scriptOptions.AddReferences(environment.GlobalType.Assembly);
        }

        var compiledScript = CSharpScript.Create<TReturn>(
            script,
            globalsType: environment.GlobalType,
            options: scriptOptions);

        compiledScript.Compile();
        var compilation = compiledScript.GetCompilation();
        var diagnostics = compilation.GetDiagnostics();

        // get the syntax tree and semantic model
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var compilationWrapper = new CompiledScript(
            compiledScript,
            syntaxTree,
            semanticModel,
            diagnostics);

        return compilationWrapper;
    }
}
