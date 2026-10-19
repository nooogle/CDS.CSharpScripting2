namespace CDS.CSharpScript2.Documentation;

/// <summary>Identifies the symbol a documentation page or link refers to.</summary>
/// <param name="DocumentationId">A Roslyn documentation comment ID, such as <c>T:System.Math</c>.</param>
public sealed record DocTarget(string DocumentationId);
