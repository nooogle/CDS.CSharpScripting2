namespace CDS.CSharpScriptUtils.APIInfo;

// Renamed for clarity, although used for various members
public class MemberDetailsInfo
{
    public string Name { get; set; }
    public string Kind { get; set; } // Added: e.g., Method, Property, Field
    public string Signature { get; set; } // Full signature
    public string ReturnType { get; set; } // Applicable for Methods, Properties, Fields, Events
    public string Summary { get; set; }
    public string Remarks { get; set; }
    public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>(); // Only for methods/constructors/indexers
}
