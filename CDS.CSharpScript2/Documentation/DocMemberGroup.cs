namespace CDS.CSharpScript2.Documentation;

/// <summary>A titled group of members on a type's page, such as its methods.</summary>
/// <param name="Heading">The group's heading.</param>
/// <param name="Members">The members in the group.</param>
public sealed record DocMemberGroup(string Heading, IReadOnlyList<DocMember> Members);
