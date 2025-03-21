using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CDS.CSharpScriptUtils
{
    /// <summary>
    /// Utility to wrap a compiled script. 
    /// </summary>
    /// <remarks>
    /// To reduce the need for client applications to have to reference the Microsoft
    /// scripting libraries we hide the compiled script in this wrapper. Only the
    /// core library can (and needs) access to this data.
    /// </remarks>
    public class CompiledScript
    {
        /// <summary>
        /// A compiled script.
        /// </summary>
        internal Script ActualScript { get; }


        /// <summary>
        /// The syntax tree for the script
        /// </summary>
        public SyntaxTree SyntaxTree { get; }


        /// <summary>
        /// The semantic model for the script
        /// </summary>
        public SemanticModel SemanticModel { get; }


        /// <summary>Compilation results</summary>
        public CompilationOutput CompilationOutput { get; }


        /// <summary>
        /// Initialise
        /// </summary>
        /// <param name="script">A compiled script</param>
        /// <param name="diagnostics">Compilation diagnostics</param>
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

            // TODO do we need to hire the compilation output or can we just put it in here?
            // TODO we we need to hide the ActualScript from the client?
        }
    }
}
