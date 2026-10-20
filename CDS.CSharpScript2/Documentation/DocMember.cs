namespace CDS.CSharpScript2.Documentation;

/// <summary>A member of a type, as listed on the type's page.</summary>
/// <param name="Name">The member's name.</param>
/// <param name="OverloadCount">How many overloads share the name.</param>
/// <param name="Target">The member's page.</param>
public sealed record DocMember(string Name, int OverloadCount, DocTarget Target);
