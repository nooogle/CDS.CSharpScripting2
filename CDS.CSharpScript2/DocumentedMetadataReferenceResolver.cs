using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace CDS.CSharpScript2;

/// <summary>
/// Resolves <c>#r</c> directives, attaching XML documentation to each resolved assembly so hover
/// text and signature help work for externally referenced libraries.
/// </summary>
/// <remarks>
/// Lookup itself is delegated to a <see cref="ScriptMetadataResolver"/> built by
/// <see cref="ScriptEnvironment"/>, which hands the same instance to both compilation paths so
/// they accept identical <c>#r</c> forms. A compilation with no resolver at all rejects every
/// <c>#r</c> with CS7099 ("Metadata references are not supported").
/// <para>
/// Resolved references are cached process-wide. See <see cref="s_cache"/> for why that matters
/// well beyond avoiding a re-read of the file.
/// </para>
/// </remarks>
internal sealed class DocumentedMetadataReferenceResolver : MetadataReferenceResolver
{
    /// <summary>
    /// Caches one <see cref="PortableExecutableReference"/> per referenced file.
    /// </summary>
    /// <remarks>
    /// Roslyn decides whether a compilation's existing reference binding can be reused by
    /// comparing reference instances. Two references created from the same file are not equal to
    /// one another, so returning a freshly created instance on every resolve forced a full rebind
    /// of the compilation on every analysis pass — measured at roughly +70 ms per pass for a
    /// single <c>#r</c>, against ~0.7 ms for the file read itself. Returning the same instance for
    /// an unchanged file is what keeps the binding cache warm.
    /// <para>
    /// Static so that several editors referencing the same assembly share one copy of its
    /// metadata rather than holding one each.
    /// </para>
    /// </remarks>
    private static readonly ConcurrentDictionary<CacheKey, CacheEntry> s_cache =
        new(CacheKeyComparer.Instance);

    private readonly MetadataReferenceResolver _inner;

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

    /// <summary>
    /// Returns a stable, documented reference for the file behind <paramref name="reference"/>.
    /// </summary>
    /// <remarks>
    /// The cached instance is reused until the file's write time or length changes, so rebuilding
    /// a referenced assembly is picked up on the next resolve. Holding the reference does not lock
    /// the file — it can still be overwritten or deleted while the editor is open.
    /// </remarks>
    private static PortableExecutableReference? WithDocumentation(PortableExecutableReference? reference)
    {
        if (reference?.FilePath is not string assemblyPath) { return reference; }

        if (!TryGetStamp(assemblyPath, out var stamp)) { return reference; }

        var key = new CacheKey(assemblyPath, reference.Properties);

        if (s_cache.TryGetValue(key, out var cached) && cached.Stamp.Equals(stamp))
        {
            return cached.Reference;
        }

        var created = TryCreate(assemblyPath, reference.Properties);

        if (created is null) { return reference; }

        // A benign race here just means two equivalent references were built and the later one
        // wins; both are valid. Assigning rather than adding keeps the cache to one entry per
        // file, so repeatedly rebuilding a referenced assembly cannot grow it without bound.
        s_cache[key] = new CacheEntry(stamp, created);

        return created;
    }

    /// <summary>
    /// Builds a reference for the given assembly, attaching XML documentation when a matching
    /// file sits beside it.
    /// </summary>
    /// <returns>The new reference, or <see langword="null"/> when the file could not be read.</returns>
    private static PortableExecutableReference? TryCreate(
        string assemblyPath,
        MetadataReferenceProperties properties)
    {
        try
        {
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            var documentation = File.Exists(xmlPath)
                ? XmlDocumentationProvider.CreateFromFile(xmlPath)
                : null;

            return MetadataReference.CreateFromFile(assemblyPath, properties, documentation);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException)
        {
            // The caller falls back to the resolver's own reference. Failing the whole
            // compilation because documentation could not be attached would be worse.
            return null;
        }
    }

    /// <summary>
    /// Reads the write time and length used to detect that a referenced file has been rebuilt.
    /// </summary>
    /// <returns><see langword="true"/> when the file was stat-ed successfully; otherwise <see langword="false"/>.</returns>
    private static bool TryGetStamp(string assemblyPath, out FileStamp stamp)
    {
        try
        {
            var info = new FileInfo(assemblyPath);

            if (!info.Exists)
            {
                stamp = default;
                return false;
            }

            stamp = new FileStamp(info.LastWriteTimeUtc, info.Length);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            stamp = default;
            return false;
        }
    }

    /// <summary>Identifies a cached reference: the same file may be referenced under different properties.</summary>
    private readonly record struct CacheKey(string Path, MetadataReferenceProperties Properties);

    /// <summary>Detects that a referenced file has changed on disk since it was cached.</summary>
    private readonly record struct FileStamp(DateTime LastWriteTimeUtc, long Length);

    /// <summary>A cached reference together with the file state it was built from.</summary>
    private readonly record struct CacheEntry(FileStamp Stamp, PortableExecutableReference Reference);

    /// <summary>Compares cache keys, treating paths case-insensitively to match Windows file naming.</summary>
    private sealed class CacheKeyComparer : IEqualityComparer<CacheKey>
    {
        public static readonly CacheKeyComparer Instance = new();

        public bool Equals(CacheKey x, CacheKey y)
            => string.Equals(x.Path, y.Path, StringComparison.OrdinalIgnoreCase)
            && x.Properties.Equals(y.Properties);

        public int GetHashCode(CacheKey obj)
            => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Path) ^ obj.Properties.GetHashCode();
    }
}
