namespace CDS.CSharpScript2.Documentation;

/// <summary>Supplies documentation pages for a script position or a link target.</summary>
public interface IDocPageSource
{
    /// <summary>Builds the page for the symbol at <paramref name="position"/>, or <see langword="null"/> when there is none.</summary>
    /// <param name="position">The offset within the script.</param>
    /// <param name="cancellationToken">A token that aborts the request.</param>
    Task<DocPage?> GetDocPageAsync(int position, CancellationToken cancellationToken);

    /// <summary>Builds the page for <paramref name="target"/>, or the unavailable page when it no longer resolves.</summary>
    /// <param name="target">The target to resolve.</param>
    /// <param name="cancellationToken">A token that aborts the request.</param>
    Task<DocPage?> GetDocPageAsync(DocTarget target, CancellationToken cancellationToken);
}
