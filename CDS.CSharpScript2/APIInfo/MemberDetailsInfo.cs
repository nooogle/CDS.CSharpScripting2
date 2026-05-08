namespace CDS.CSharpScript2.APIInfo;

// Renamed for clarity, although used for various members
public class MemberDetailsInfo
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>(); 
}
