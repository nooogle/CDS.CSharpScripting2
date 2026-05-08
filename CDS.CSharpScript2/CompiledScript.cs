using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CDS.CSharpScript2
{
    /// <summary>
    /// Utility to wrap a compiled script. 
    /// </summary>
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


        public IReadOnlyList<Classification.ClassifiedSymbol> ClassifiedSpans { get; private set; } = new List<Classification.ClassifiedSymbol>();


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

        public CompiledScript WithClassifiedSpans(
            IReadOnlyList<Classification.ClassifiedSymbol> classifiedSpans)
        {
            var copy = (CompiledScript)this.MemberwiseClone();
            copy.ClassifiedSpans = classifiedSpans;
            return copy;
        }
    }
}
