namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// The result of an API info lookup at a specific cursor position.
/// <see cref="TypeInfo"/> describes the containing or referenced type.
/// <see cref="MemberInfos"/> lists the relevant members or overloads — empty when the
/// result is type-only (e.g. hovering a type name rather than a member call).
/// </summary>
public sealed class APIInfoResult
{
    /// <summary>Metadata for the type relevant to the cursor position.</summary>
    public DetailedTypeInfo? TypeInfo { get; }

    /// <summary>
    /// Members or overloads relevant to the cursor position.
    /// Never null; empty when only type-level info is available.
    /// </summary>
    public IReadOnlyList<MemberDetailsInfo> MemberInfos { get; }

    /// <summary>Creates a type-only result with no member info.</summary>
    public APIInfoResult(DetailedTypeInfo? typeInfo)
        : this(typeInfo, null) { }

    /// <summary>Creates a result with both type and member info.</summary>
    public APIInfoResult(DetailedTypeInfo? typeInfo, IEnumerable<MemberDetailsInfo>? memberInfos)
    {
        TypeInfo = typeInfo;
        MemberInfos = memberInfos?.ToList() ?? [];
    }
}
