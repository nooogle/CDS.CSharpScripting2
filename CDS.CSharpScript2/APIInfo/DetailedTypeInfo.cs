namespace CDS.CSharpScript2.APIInfo;

public class DetailedTypeInfo
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Accessibility { get; set; } = string.Empty;
    public string BaseType { get; set; } = string.Empty;
    public List<string> Interfaces { get; set; } = new List<string>();
    public string Summary { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string TypeKind { get; set; } = string.Empty;
}
