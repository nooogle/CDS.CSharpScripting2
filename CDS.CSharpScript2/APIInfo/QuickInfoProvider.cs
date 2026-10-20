using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.QuickInfo;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Produces <see cref="QuickInfo"/> from Roslyn's quick info service — the source of Visual
/// Studio's hover — keeping only its compact sections.
/// </summary>
internal static class QuickInfoProvider
{
    internal static async Task<QuickInfo?> GetAsync(Document document, int position, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var service = QuickInfoService.GetService(document);
        if (service == null)
            return null;

        var item = await service.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (item == null)
            return null;

        // Roslyn also supplies returns, exceptions, type parameters and more; only the first two
        // sections make the compact view.
        var signature = GetSectionText(item, QuickInfoSectionKinds.Description);
        if (signature.Length == 0)
            return null;

        return new QuickInfo
        {
            Signature = signature,
            Summary = GetSectionText(item, QuickInfoSectionKinds.DocumentationComments),
            Start = item.Span.Start,
            Length = item.Span.Length,
        };
    }

    private static string GetSectionText(QuickInfoItem item, string kind)
    {
        return item.Sections.FirstOrDefault(section => section.Kind == kind)?.Text.Trim() ?? string.Empty;
    }
}
