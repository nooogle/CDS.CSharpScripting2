namespace CDS.CSharpScriptUtils.APIInfo
{
    public class MethodOverloadInfo
    {
        public string Name { get; set; }
        public string Signature { get; set; }
        public string ReturnType { get; set; }
        public string Summary { get; set; }
        public string Remarks { get; set; }
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
    }
}
