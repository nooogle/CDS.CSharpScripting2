using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace CDS.CSharpScript2;

/// <summary>
/// Stateless analysis service for a <see cref="ScriptContext"/>.
/// All methods use the Roslyn workspace document — no execution-compilation occurs.
/// Construct a new instance whenever the context changes.
/// </summary>
public class ScriptAnalyser
{
    private readonly ScriptContext _context;
    private readonly Classification.ClassificationMapper _classificationMapper = new();

    /// <param name="context">The script context to analyse.</param>
    public ScriptAnalyser(ScriptContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Returns all compile-time diagnostics (errors and warnings) using the workspace
    /// semantic model. No execution-compilation is triggered.
    /// </summary>
    public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(CancellationToken ct = default)
    {
        var compilation = await _context.Document.Project
            .GetCompilationAsync(ct).ConfigureAwait(false);

        return compilation?.GetDiagnostics(ct) ?? ImmutableArray<Diagnostic>.Empty;
    }

    /// <summary>Returns the syntax tree for the current script text.</summary>
    public async Task<SyntaxTree?> GetSyntaxTreeAsync(CancellationToken ct = default)
        => await _context.Document.GetSyntaxTreeAsync(ct).ConfigureAwait(false);

    /// <summary>Returns the semantic model for the current script text.</summary>
    public async Task<SemanticModel?> GetSemanticModelAsync(CancellationToken ct = default)
        => await _context.Document.GetSemanticModelAsync(ct).ConfigureAwait(false);

    /// <summary>Returns classified symbol spans for the entire script.</summary>
    public async Task<IReadOnlyList<Classification.ClassifiedSymbol>> GetClassificationsAsync(
        CancellationToken ct = default)
        => await GetClassificationsAsync(0, _context.ScriptText.Length, ct).ConfigureAwait(false);

    /// <summary>Returns classified symbol spans for the given text range.</summary>
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

    /// <summary>Returns code completion suggestions at the given cursor position.</summary>
    public async Task<ImmutableArray<CompletionItem>> GetCompletionsAsync(int position)
        => await CodeCompletion.Manager.GetAsync(
            scriptText: _context.ScriptText,
            document: _context.Document,
            cursorPosition: position).ConfigureAwait(false);

    /// <summary>
    /// Returns API information (type info, member overloads, XML docs) at the given position.
    /// Returns null when no symbol is found at that position.
    /// </summary>
    public async Task<APIInfo.APIInfoResult?> GetAPIInfoAsync(int position)
    {
        var syntaxTree = await GetSyntaxTreeAsync().ConfigureAwait(false);
        var semanticModel = await GetSemanticModelAsync().ConfigureAwait(false);

        if (syntaxTree == null || semanticModel == null)
            return null;

        return APIInfo.APIInfoService.Get(syntaxTree, semanticModel, position) as APIInfo.APIInfoResult;
    }
}
