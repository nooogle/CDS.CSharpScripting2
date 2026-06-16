namespace CDS.CSharpScript2.APIInfo;

/// <summary>A single parameter extracted from a method or indexer symbol.</summary>
public sealed record ParameterInfo(string Name, string Type, string? DefaultValue = null, string? Documentation = null);
