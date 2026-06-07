using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Immutable;

namespace CDS.CSharpScript2;

/// <summary>
/// Wraps the Roslyn script together with the compilation artifacts used for analysis and execution.
/// </summary>
internal class CompiledScript
{
    /// <summary>
    /// Gets the underlying Roslyn script object used for execution.
    /// </summary>
    internal Script ActualScript { get; }

    /// <summary>
    /// Gets the syntax tree produced during compilation.
    /// </summary>
    public SyntaxTree SyntaxTree { get; }

    /// <summary>
    /// Gets the semantic model produced during compilation.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// Gets diagnostics and summary counts from the compilation.
    /// </summary>
    public CompilationOutput CompilationOutput { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledScript"/> class.
    /// </summary>
    /// <param name="script">The Roslyn script object.</param>
    /// <param name="syntaxTree">The syntax tree produced by compilation.</param>
    /// <param name="semanticModel">The semantic model produced by compilation.</param>
    /// <param name="diagnostics">The diagnostics produced by compilation.</param>
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
