namespace CDS.CSharpScript2.Documentation;

/// <summary>A named entry with a description, such as a parameter or an exception.</summary>
/// <param name="Name">The name: a single <see cref="DocRunKind.Code"/> run for a parameter, a single <see cref="DocRunKind.Link"/> run for an exception.</param>
/// <param name="Description">The description of the entry.</param>
public sealed record DocNamedItem(IReadOnlyList<DocRun> Name, IReadOnlyList<DocRun> Description);
