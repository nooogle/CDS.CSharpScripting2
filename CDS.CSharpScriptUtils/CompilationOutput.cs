using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CDS.CSharpScriptUtils
{
    /// <summary>
    /// Basic compilation results
    /// </summary>
    public class CompilationOutput
    {
        /// <summary>
        /// Compilation diagnostics
        /// </summary>
        public ImmutableArray<string> Messages { get; }

        /// <summary>Number of compilation warnings</summary>
        public int WarningCount { get; }

        /// <summary>Number of compilation errors</summary>
        public int ErrorCount { get; }

        /// <summary>
        /// All diagnostics
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="diagnostics">The compilation diagnostics</param>
        /// <exception cref="ArgumentNullException">Thrown when diagnostics is null</exception>
        internal CompilationOutput(ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.IsDefault)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            Diagnostics = diagnostics;

            var output = diagnostics.Select(d => d.ToString()).ToImmutableArray();
            Messages = output;

            WarningCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
            ErrorCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        }
    }
}
