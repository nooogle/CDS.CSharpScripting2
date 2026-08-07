using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Immutable;

namespace CDS.CSharpScript2;

/// <summary>
/// Resolves <c>#r</c> directives for the editor's workspace compilation, attaching XML
/// documentation to each resolved assembly so hover text and signature help work for
/// externally referenced libraries.
/// </summary>
/// <remarks>
/// Resolution is delegated to <see cref="ScriptMetadataResolver.Default"/> — the same resolver the
/// execution path picks up from <c>ScriptOptions.Default</c>, so both paths accept the same
/// <c>#r</c> forms. A compilation with no resolver at all rejects every <c>#r</c> with
/// CS7099 ("Metadata references are not supported").
/// </remarks>
internal sealed class DocumentedMetadataReferenceResolver : MetadataReferenceResolver
{
    private readonly MetadataReferenceResolver _inner;

    /// <summary>
    /// Gets the shared instance wrapping <see cref="ScriptMetadataResolver.Default"/>.
    /// </summary>
    public static DocumentedMetadataReferenceResolver Default { get; } =
        new DocumentedMetadataReferenceResolver(ScriptMetadataResolver.Default);

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentedMetadataReferenceResolver"/> class.
    /// </summary>
    /// <param name="inner">The resolver that performs the actual lookup.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> is <see langword="null"/>.</exception>
    public DocumentedMetadataReferenceResolver(MetadataReferenceResolver inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc/>
    public override bool ResolveMissingAssemblies => _inner.ResolveMissingAssemblies;

    /// <inheritdoc/>
    public override PortableExecutableReference? ResolveMissingAssembly(
        MetadataReference definition,
        AssemblyIdentity referenceIdentity)
        => WithDocumentation(_inner.ResolveMissingAssembly(definition, referenceIdentity));

    /// <inheritdoc/>
    public override ImmutableArray<PortableExecutableReference> ResolveReference(
        string reference,
        string? baseFilePath,
        MetadataReferenceProperties properties)
    {
        var resolved = _inner.ResolveReference(reference, baseFilePath, properties);

        if (resolved.IsDefaultOrEmpty) { return resolved; }

        return resolved
            .Select(r => WithDocumentation(r) ?? r)
            .ToImmutableArray();
    }

    /// <inheritdoc/>
    public override bool Equals(object? other)
        => other is DocumentedMetadataReferenceResolver resolver && _inner.Equals(resolver._inner);

    /// <inheritdoc/>
    public override int GetHashCode() => _inner.GetHashCode();

    private static PortableExecutableReference? WithDocumentation(PortableExecutableReference? reference)
    {
        if (reference?.FilePath is not string assemblyPath) { return reference; }

        var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        if (!File.Exists(xmlPath)) { return reference; }

        return MetadataReference.CreateFromFile(
            assemblyPath,
            reference.Properties,
            XmlDocumentationProvider.CreateFromFile(xmlPath));
    }
}
