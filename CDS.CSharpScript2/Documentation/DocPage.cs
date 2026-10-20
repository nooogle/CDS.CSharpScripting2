namespace CDS.CSharpScript2.Documentation;

/// <summary>A UI-neutral documentation page describing one symbol.</summary>
/// <remarks>
/// Record equality is not deep: the list properties compare by reference, so two pages built
/// separately for the same symbol are not equal. Compare <see cref="Target"/> to identify a page.
/// </remarks>
public sealed record DocPage
{
    /// <summary>The symbol the page describes; <see langword="null"/> for variables and parameters without a documentation ID.</summary>
    public DocTarget? Target { get; init; }

    /// <summary>What kind of symbol this is, such as <c>Method · Math</c>, with links where useful.</summary>
    public IReadOnlyList<DocRun> KindLine { get; init; } = [];

    /// <summary>The page title, such as <c>Math.Round</c>.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The declaration, such as <c>public static double Round(double value, int digits)</c>.</summary>
    public string Signature { get; init; } = string.Empty;

    /// <summary>The summary text.</summary>
    public IReadOnlyList<DocRun> Summary { get; init; } = [];

    /// <summary>The documented parameters.</summary>
    public IReadOnlyList<DocNamedItem> Parameters { get; init; } = [];

    /// <summary>The description of the return value.</summary>
    public IReadOnlyList<DocRun> Returns { get; init; } = [];

    /// <summary>The documented exceptions.</summary>
    public IReadOnlyList<DocNamedItem> Exceptions { get; init; } = [];

    /// <summary>The remarks text.</summary>
    public IReadOnlyList<DocRun> Remarks { get; init; } = [];

    /// <summary>The overloads of a method, in declaration order.</summary>
    public IReadOnlyList<DocOverload> Overloads { get; init; } = [];

    /// <summary>The grouped members of a type.</summary>
    public IReadOnlyList<DocMemberGroup> MemberGroups { get; init; } = [];

    /// <summary>Whether the target could not be resolved, so the page has no content.</summary>
    public bool IsUnavailable { get; init; }

    /// <summary>Creates the placeholder page shown when <paramref name="target"/> cannot be resolved.</summary>
    /// <param name="target">The target that could not be resolved.</param>
    public static DocPage Unavailable(DocTarget target)
        => new() { Target = target, Title = target.DocumentationId, IsUnavailable = true };
}
