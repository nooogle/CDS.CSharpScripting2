using System.Collections.Generic;

namespace ConsoleScratchFramework.ForLib
{
    public class MethodOverloadInfo
    {
        public string Name { get; set; }
        public string Signature { get; set; }
        public IList<ParameterInfo> Parameters { get; set; }
        public string ReturnType { get; set; }
        public string Summary { get; set; }
        public string Remarks { get; set; }

        public MethodOverloadInfo()
        {
            Parameters = new List<ParameterInfo>();
        }
    }
}
