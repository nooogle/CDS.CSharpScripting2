using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CDS.CSharpScript2
{
    /// <summary>
    /// Represents the diagnostics and summary counts produced by a script compilation.
    /// </summary>
    public class CompilationOutput
    {
        /// <summary>
        /// Gets preformatted diagnostic messages suitable for display in logs or output panes.
        /// </summary>
        public ImmutableArray<string> Messages { get; }

        /// <summary>
        /// Gets the number of warning diagnostics.
        /// </summary>
        public int WarningCount { get; }

        /// <summary>
        /// Gets the number of error diagnostics.
        /// </summary>
        public int ErrorCount { get; }

        /// <summary>
        /// Gets all diagnostics returned by Roslyn for the compilation.
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilationOutput"/> class.
        /// </summary>
        /// <param name="diagnostics">The compilation diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="diagnostics"/> is default.</exception>
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
