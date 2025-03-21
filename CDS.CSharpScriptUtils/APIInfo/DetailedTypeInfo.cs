namespace CDS.CSharpScriptUtils.APIInfo
{
    public class DetailedTypeInfo
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Accessibility { get; set; }
        public string BaseType { get; set; }
        public List<string> Interfaces { get; set; } = new List<string>();
        public string Summary { get; set; }
        public string Remarks { get; set; }
    }
}
