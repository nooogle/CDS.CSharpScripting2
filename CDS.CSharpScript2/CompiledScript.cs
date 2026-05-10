using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Immutable;

namespace CDS.CSharpScript2;

/// <summary>
/// Wraps a Roslyn-compiled script together with its syntax tree, semantic model, and diagnostics.
/// </summary>
internal class CompiledScript
{
    /// <summary>The underlying Roslyn script object used for execution.</summary>
    internal Script ActualScript { get; }

    /// <summary>The syntax tree produced by compilation.</summary>
    public SyntaxTree SyntaxTree { get; }

    /// <summary>The semantic model produced by compilation.</summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>Diagnostics and summary counts from the compilation.</summary>
    public CompilationOutput CompilationOutput { get; }

    public CompiledScript(
        Script script,
        SyntaxTree syntaxTree,
        SemanticModel semanticModel,
        ImmutableArray<Diagnostic> diagnostics)
    {
        ActualScript = script;
        SyntaxTree = syntaxTree;
        SemanticModel = semanticModel;
        CompilationOutput = new CompilationOutput(diagnostics);
    }
}
