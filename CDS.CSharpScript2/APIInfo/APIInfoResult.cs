namespace CDS.CSharpScript2.APIInfo;

public class APIInfoResult : IAPIInfoResult
{
    public DetailedTypeInfo? TypeInfo { get; } // Info about the containing type or the type itself
    public IEnumerable<MemberDetailsInfo>? MemberInfos { get; } // Info about specific member(s) or overloads

    public APIInfoResult(DetailedTypeInfo? typeInfo, IEnumerable<MemberDetailsInfo>? memberInfos)
    {
        TypeInfo = typeInfo;
        MemberInfos = memberInfos ?? Enumerable.Empty<MemberDetailsInfo>();
    }
}

