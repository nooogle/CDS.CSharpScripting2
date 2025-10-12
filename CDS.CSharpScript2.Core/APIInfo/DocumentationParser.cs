using Microsoft.CodeAnalysis;
using System.Xml;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Parses XML documentation from symbols.
/// </summary>
public static class DocumentationParser
{
    public static (string Summary, string Remarks, Dictionary<string, string> ParamDocs) Parse(ISymbol symbol)
    {
        if (symbol == null) return (null, null, new Dictionary<string, string>());
        string xmlDocs = symbol.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: System.Threading.CancellationToken.None);
        if (string.IsNullOrEmpty(xmlDocs))
        {
            return (null, null, new Dictionary<string, string>());
        }
        try
        {
            using (var reader = new System.IO.StringReader(xmlDocs))
            {
                var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };
                using (var xmlReader = XmlReader.Create(reader, settings))
                {
                    string summary = null;
                    string remarks = null;
                    var paramDocs = new Dictionary<string, string>();
                    string currentParamName = null;
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            switch (xmlReader.Name.ToLowerInvariant())
                            {
                                case "summary":
                                    summary = xmlReader.ReadInnerXml().Trim();
                                    break;
                                case "remarks":
                                    remarks = xmlReader.ReadInnerXml().Trim();
                                    break;
                                case "param":
                                    currentParamName = xmlReader.GetAttribute("name");
                                    if (currentParamName != null)
                                    {
                                        string paramValue = xmlReader.ReadInnerXml().Trim();
                                        if (!paramDocs.ContainsKey(currentParamName))
                                        {
                                            paramDocs.Add(currentParamName, paramValue);
                                        }
                                    }
                                    currentParamName = null;
                                    break;
                            }
                        }
                    }
                    return (summary, remarks, paramDocs);
                }
            }
        }
        catch (XmlException)
        {
            return (null, null, new Dictionary<string, string>());
        }
        catch (Exception)
        {
            return (null, null, new Dictionary<string, string>());
        }
    }
}
