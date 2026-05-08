namespace CDS.CSharpScript2.APIInfo;

public interface IAPIInfoResult
{
    IEnumerable<MemberDetailsInfo>? MemberInfos { get; }
    DetailedTypeInfo? TypeInfo { get; }
}