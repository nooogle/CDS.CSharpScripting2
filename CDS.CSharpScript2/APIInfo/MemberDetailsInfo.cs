namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Metadata for a single member (method, property, field, event, parameter, or local).
/// When a method has overloads, one <see cref="MemberDetailsInfo"/> is produced per overload.
/// </summary>
public sealed record MemberDetailsInfo
{
    /// <summary>The simple member name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The kind of member: Method, Property, Field, Event, Parameter, etc.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>The full display signature of the member.</summary>
    public string Signature { get; init; } = string.Empty;

    /// <summary>Display string of the return type (empty for constructors and void methods).</summary>
    public string ReturnType { get; init; } = string.Empty;

    /// <summary>Content of the XML <c>&lt;summary&gt;</c> documentation tag.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Content of the XML <c>&lt;remarks&gt;</c> documentation tag.</summary>
    public string Remarks { get; init; } = string.Empty;

    /// <summary>Parameters for methods and indexers; empty for other member kinds.</summary>
    public IReadOnlyList<ParameterInfo> Parameters { get; init; } = [];
}
