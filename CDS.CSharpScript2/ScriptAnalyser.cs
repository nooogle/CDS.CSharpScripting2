using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace CDS.CSharpScript2;

/// <summary>
/// Stateless analysis service for a <see cref="ScriptContext"/>.
/// All methods operate on the Roslyn workspace document and project.
/// This avoids the library's execution compilation pipeline, but some operations
/// still rely on Roslyn's semantic model or design-time compilation services.
/// Construct a new instance whenever the context changes.
/// </summary>
public class ScriptAnalyser
{
    private readonly ScriptContext _context;
    private readonly Classification.ClassificationMapper _classificationMapper = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptAnalyser"/> class.
    /// </summary>
    /// <param name="context">The script context to analyse.</param>
    public ScriptAnalyser(ScriptContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Returns all compile-time diagnostics (errors and warnings) using Roslyn's
    /// workspace project compilation.
    /// </summary>
    /// <remarks>
    /// This does not invoke the library's execution-compilation path, but it does ask
    /// Roslyn to produce a project compilation in order to calculate diagnostics.
    /// </remarks>
    public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(CancellationToken ct = default)
    {
        var compilation = await _context.Document.Project
            .GetCompilationAsync(ct).ConfigureAwait(false);

        return compilation?.GetDiagnostics(ct) ?? ImmutableArray<Diagnostic>.Empty;
    }

    /// <summary>
    /// Returns the syntax tree for the current script text.
    /// </summary>
    public async Task<SyntaxTree?> GetSyntaxTreeAsync(CancellationToken ct = default)
        => await _context.Document.GetSyntaxTreeAsync(ct).ConfigureAwait(false);

    /// <summary>
    /// Returns the semantic model for the current script text.
    /// </summary>
    /// <remarks>
    /// Roslyn may build semantic state for the document to satisfy this request.
    /// </remarks>
    public async Task<SemanticModel?> GetSemanticModelAsync(CancellationToken ct = default)
        => await _context.Document.GetSemanticModelAsync(ct).ConfigureAwait(false);

    /// <summary>
    /// Returns classified symbol spans for the entire script.
    /// </summary>
    /// <remarks>
    /// Classification is provided by Roslyn workspace services and may depend on
    /// semantic analysis for some classifications.
    /// </remarks>
    public async Task<IReadOnlyList<Classification.ClassifiedSymbol>> GetClassificationsAsync(
        CancellationToken ct = default)
        => await GetClassificationsAsync(0, _context.ScriptText.Length, ct).ConfigureAwait(false);

    /// <summary>
    /// Returns classified symbol spans for the given text range.
    /// </summary>
    /// <remarks>
    /// Classification is provided by Roslyn workspace services and may depend on
    /// semantic analysis for some classifications.
    /// </remarks>
    public async Task<IReadOnlyList<Classification.ClassifiedSymbol>> GetClassificationsAsync(
        int spanStart,
        int spanLength,
        CancellationToken ct = default)
    {
        var span = new TextSpan(spanStart, spanLength);

        var spans = await Microsoft.CodeAnalysis.Classification.Classifier
            .GetClassifiedSpansAsync(_context.Document, span, ct)
            .ConfigureAwait(false);

        var ignoreList = Microsoft.CodeAnalysis.Classification.ClassificationTypeNames.AdditiveTypeNames;

        return spans
            .Where(s => !ignoreList.Contains(s.ClassificationType))
            .Select(s =>
            {
                if (_classificationMapper.Map.TryGetValue(s.ClassificationType, out var classification))
                    return new Classification.ClassifiedSymbol(s.TextSpan.Start, s.TextSpan.Length, classification);

                return new Classification.ClassifiedSymbol(s.TextSpan.Start, s.TextSpan.Length, Classification.SymbolClassification.Keyword);
            })
            .ToList();
    }

    /// <summary>
    /// Returns code completion suggestions at the given cursor position.
    /// </summary>
    /// <remarks>
    /// Completions are produced from the Roslyn workspace document and may rely on
    /// semantic information for accurate results.
    /// </remarks>
    public async Task<ImmutableArray<CompletionItem>> GetCompletionsAsync(int position)
        => await CodeCompletion.Manager.GetAsync(
            scriptText: _context.ScriptText,
            document: _context.Document,
            cursorPosition: position).ConfigureAwait(false);

    /// <summary>
    /// Returns API information (type info, member overloads, XML docs) at the given position.
    /// Returns null when no symbol is found at that position.
    /// </summary>
    /// <remarks>
    /// This uses the Roslyn syntax tree and semantic model. It does not invoke the
    /// library's execution-compilation path.
    /// </remarks>
    public async Task<APIInfo.APIInfoResult?> GetAPIInfoAsync(int position)
    {
        var syntaxTree = await GetSyntaxTreeAsync().ConfigureAwait(false);
        var semanticModel = await GetSemanticModelAsync().ConfigureAwait(false);

        if (syntaxTree == null || semanticModel == null)
            return null;

        return APIInfo.APIInfoService.Get(syntaxTree, semanticModel, position) as APIInfo.APIInfoResult;
    }

    /// <summary>
    /// Returns the active argument index and opening-paren position when the cursor
    /// sits inside a method or indexer argument list; otherwise returns <see langword="null"/>.
    /// Only the syntax tree is required — no semantic analysis is performed.
    /// </summary>
    public async Task<APIInfo.CallTipContext?> GetCallTipContextAsync(int position)
    {
        var syntaxTree = await GetSyntaxTreeAsync().ConfigureAwait(false);

        if (syntaxTree is null)
            return null;

        return APIInfo.CallTipService.GetContext(syntaxTree, position);
    }
}
