using Microsoft.CodeAnalysis;
using System.Xml.Linq;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Parses XML documentation comments from Roslyn symbols.
/// </summary>
internal static class DocumentationParser
{
    /// <summary>
    /// Extracts summary, remarks, param, and typeparam documentation from the given symbol.
    /// Returns empty strings and empty dictionaries when the symbol is null or carries no docs.
    /// </summary>
    internal static (string Summary, string Remarks,
        IReadOnlyDictionary<string, string> ParamDocs,
        IReadOnlyDictionary<string, string> TypeParamDocs) Parse(ISymbol? symbol)
    {
        var xml = symbol?.GetDocumentationCommentXml(expandIncludes: true);
        if (string.IsNullOrWhiteSpace(xml))
            return (string.Empty, string.Empty, Empty, Empty);

        try
        {
            // Wrap in a root element to handle both raw fragments and <member>-wrapped formats.
            var doc = XDocument.Parse($"<root>{xml}</root>");

            var summary = doc.Descendants("summary").FirstOrDefault()?.Value.Trim() ?? string.Empty;
            var remarks = doc.Descendants("remarks").FirstOrDefault()?.Value.Trim() ?? string.Empty;

            var paramDocs = doc.Descendants("param")
                .Where(e => e.Attribute("name") != null)
                .ToDictionary(e => e.Attribute("name")!.Value, e => e.Value.Trim());

            var typeParamDocs = doc.Descendants("typeparam")
                .Where(e => e.Attribute("name") != null)
                .ToDictionary(e => e.Attribute("name")!.Value, e => e.Value.Trim());

            return (summary, remarks, paramDocs, typeParamDocs);
        }
        catch (System.Xml.XmlException)
        {
            return (string.Empty, string.Empty, Empty, Empty);
        }
    }

    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>();
}
