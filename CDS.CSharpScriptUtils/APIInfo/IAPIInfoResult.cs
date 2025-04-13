
namespace CDS.CSharpScriptUtils.APIInfo
{
    public interface IAPIInfoResult
    {
        IEnumerable<MemberDetailsInfo> MemberInfos { get; }
        DetailedTypeInfo TypeInfo { get; }
    }
}