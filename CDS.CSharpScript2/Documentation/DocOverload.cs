namespace CDS.CSharpScript2.Documentation;

/// <summary>One overload of a method, as listed on its page.</summary>
/// <param name="Label">The overload's name and parameter types, such as <c>Round(double, int)</c>.</param>
/// <param name="Target">The overload's page.</param>
/// <param name="IsCurrent">Whether this is the overload the page describes.</param>
public sealed record DocOverload(string Label, DocTarget Target, bool IsCurrent);
