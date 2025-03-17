namespace CDS.CSScripting2.APIInfo
{
    public class APIInfo
    {
        public DetailedTypeInfo TypeInfo { get; }
        public IEnumerable<MethodOverloadInfo> MemberInfos { get; }


        public APIInfo(DetailedTypeInfo typeInfo, IEnumerable<MethodOverloadInfo> memberInfos)
        {
            TypeInfo = typeInfo;
            MemberInfos = memberInfos;
        }

    }
}
