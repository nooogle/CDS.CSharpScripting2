namespace CDS.CSharpScript2.Documentation;

/// <summary>How a <see cref="DocRun"/> is presented.</summary>
public enum DocRunKind
{
    /// <summary>Ordinary prose.</summary>
    Text,

    /// <summary>Code, such as a parameter name or keyword, shown in a code style.</summary>
    Code,

    /// <summary>A reference to another documentation page, carrying a <see cref="DocTarget"/>.</summary>
    Link,
}
