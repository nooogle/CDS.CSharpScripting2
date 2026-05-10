namespace CDS.CSharpScript2.APIInfo;

/// <summary>Type-level metadata extracted from a Roslyn named type symbol.</summary>
public sealed record DetailedTypeInfo
{
    /// <summary>The simple type name (without namespace).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The fully-qualified namespace of the type.</summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>The kind of type: Class, Struct, Interface, Enum, etc.</summary>
    public string TypeKind { get; init; } = string.Empty;

    /// <summary>Declared accessibility: Public, Internal, etc.</summary>
    public string Accessibility { get; init; } = string.Empty;

    /// <summary>Display string of the base type, or empty for types with no explicit base.</summary>
    public string BaseType { get; init; } = string.Empty;

    /// <summary>Display strings of directly implemented interfaces.</summary>
    public IReadOnlyList<string> Interfaces { get; init; } = [];

    /// <summary>Content of the XML <c>&lt;summary&gt;</c> documentation tag.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Content of the XML <c>&lt;remarks&gt;</c> documentation tag.</summary>
    public string Remarks { get; init; } = string.Empty;
}
