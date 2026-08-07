using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Immutable;
using System.Reflection;

namespace CDS.CSharpScript2;

/// <summary>
/// Represents the immutable configuration used when compiling and running scripts,
/// including imported namespaces, referenced assemblies, the optional globals type, and how
/// <c>#r</c> and <c>#load</c> directives are resolved.
/// </summary>
public class ScriptEnvironment
{
    private ImmutableList<string> namespaceNames;
    private ImmutableList<Assembly> references;
    private Type? globalType;
    private string? baseDirectory;
    private static readonly ScriptEnvironment defaultInstance;
    private static readonly bool isNetFramework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Contains(".NET Framework");

    /// <summary>
    /// Gets the default script environment instance.
    /// </summary>
    /// <remarks>
    /// The default environment imports <see cref="System"/> and references the assembly containing <see cref="Console"/>.
    /// </remarks>
    public static ScriptEnvironment Default => defaultInstance;

    /// <summary>
    /// Gets a value indicating whether the current runtime is .NET Framework.
    /// </summary>
    public static bool IsNetFramework => isNetFramework;

    /// <summary>
    /// Gets the namespace imports that will be available to compiled scripts.
    /// </summary>
    public IEnumerable<string> NamespaceNames => namespaceNames;

    /// <summary>
    /// Gets the assembly references that will be passed to the Roslyn scripting engine.
    /// </summary>
    public IEnumerable<Assembly> References => references;

    /// <summary>
    /// Gets the globals type exposed to scripts, or <see langword="null"/> when no globals object is configured.
    /// </summary>
    public Type? GlobalType => globalType;

    /// <summary>
    /// Gets the directory that relative <c>#r</c> and <c>#load</c> paths are resolved against,
    /// or <see langword="null"/> when only absolute paths are supported.
    /// </summary>
    public string? BaseDirectory => baseDirectory;

    /// <summary>
    /// Gets the resolver used for <c>#r</c> directives.
    /// </summary>
    /// <remarks>
    /// Both compilation paths read this from the environment, so the editor and the execution
    /// engine always accept exactly the same directives.
    /// </remarks>
    internal MetadataReferenceResolver MetadataResolver { get; }

    /// <summary>
    /// Gets the resolver used for <c>#load</c> directives.
    /// </summary>
    /// <remarks>
    /// Both compilation paths read this from the environment, so the editor and the execution
    /// engine always accept exactly the same directives.
    /// </remarks>
    internal SourceReferenceResolver SourceResolver { get; }

    static ScriptEnvironment()
    {
        var defaultNamespaces = (new[] { typeof(object).Namespace! }).ToImmutableList();

        var defaultReferences = new[]
        {
            //typeof(object).Assembly,
            typeof(Console).Assembly,
        }.ToImmutableList();

        defaultInstance = new ScriptEnvironment(
            namespaceNames: defaultNamespaces,
            references: defaultReferences,
            globalType: null,
            baseDirectory: null);
    }

    private ScriptEnvironment(
        ImmutableList<string> namespaceNames,
        ImmutableList<Assembly> references,
        Type? globalType,
        string? baseDirectory)
    {
        this.namespaceNames = namespaceNames.Distinct().ToImmutableList();
        this.references = references.Distinct().ToImmutableList();
        this.globalType = globalType;
        this.baseDirectory = baseDirectory;

        // Built once here so both compilation paths share one configuration. The resolvers
        // compare by value, so two environments with the same base directory stay interchangeable
        // as far as Roslyn's compilation caching is concerned.
        MetadataResolver = new DocumentedMetadataReferenceResolver(
            baseDirectory is null
                ? ScriptMetadataResolver.Default
                : ScriptMetadataResolver.Default.WithBaseDirectory(baseDirectory));

        SourceResolver = baseDirectory is null
            ? SourceFileResolver.Default
            : new SourceFileResolver(ImmutableArray<string>.Empty, baseDirectory);
    }

    /// <summary>
    /// Returns a new instance with the namespace containing <paramref name="type"/> added to the imports.
    /// </summary>
    /// <param name="type">The type whose namespace will be added.</param>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> does not belong to a namespace.</exception>
    public ScriptEnvironment WithAdditionalNamespaceType(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        string typeNamespace = type.Namespace ?? throw new ArgumentException($"The type '{type.FullName}' does not have a namespace.", nameof(type));

        return new ScriptEnvironment(
            namespaceNames.Add(typeNamespace),
            references,
            globalType,
            baseDirectory);
    }

    /// <summary>
    /// Returns a new instance with the drawing-related assemblies required for common <c>System.Drawing</c> usage.
    /// </summary>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    /// <remarks>
    /// On .NET Framework this adds the assembly containing <see cref="System.Drawing.Point"/>.
    /// On modern .NET it loads both <c>System.Drawing</c> and <c>System.Drawing.Primitives</c>.
    /// </remarks>
    public ScriptEnvironment WithDrawingReferences()
    {
        if (IsNetFramework)
        {
            var systemDrawingFullName = typeof(System.Drawing.Point).Assembly;

            return new ScriptEnvironment(
                namespaceNames,
                references.Add(systemDrawingFullName),
                globalType,
                baseDirectory);
        }

        var newReferenceNames = new[] { "System.Drawing", "System.Drawing.Primitives" };
        var newAssemblies = newReferenceNames.Select(LoadAssembly);

        return new ScriptEnvironment(
            namespaceNames: namespaceNames,
            references: references.AddRange(newAssemblies),
            globalType: globalType,
            baseDirectory: baseDirectory);
    }

    /// <summary>
    /// Returns a new instance with an additional namespace import.
    /// </summary>
    /// <param name="namespaceName">The namespace name to add.</param>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithAdditionalNamespaceName(string namespaceName)
    {
        return new ScriptEnvironment(
            namespaceNames.Add(namespaceName),
            references,
            globalType,
            baseDirectory);
    }

    /// <summary>
    /// Returns a new instance with an additional assembly reference loaded by name.
    /// </summary>
    /// <param name="referenceName">
    /// A simple name (<c>"System.Linq"</c>) or a full display name.
    /// </param>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="referenceName"/> is empty, or when no matching assembly can be loaded.
    /// </exception>
    public ScriptEnvironment WithAdditionalReferenceName(string referenceName)
    {
        if (string.IsNullOrWhiteSpace(referenceName))
        {
            throw new ArgumentException("A reference name is required.", nameof(referenceName));
        }

        return new ScriptEnvironment(
            namespaceNames,
            references.Add(LoadAssembly(referenceName)),
            globalType,
            baseDirectory);
    }

    /// <summary>
    /// Returns a new instance that resolves relative <c>#r</c> and <c>#load</c> paths against
    /// <paramref name="directory"/>.
    /// </summary>
    /// <param name="directory">An absolute path to the directory scripts are resolved against.</param>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    /// <remarks>
    /// Typically the folder the user's script file lives in, so a script can say
    /// <c>#r "libs/Helper.dll"</c> rather than hard-coding a machine-specific absolute path.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="directory"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="directory"/> is not an absolute path.</exception>
    public ScriptEnvironment WithBaseDirectory(string directory)
    {
        if (directory == null)
        {
            throw new ArgumentNullException(nameof(directory));
        }

        // Roslyn's resolvers reject anything relative, with a less helpful message than this one.
        if (!Path.IsPathRooted(directory))
        {
            throw new ArgumentException(
                $"The base directory must be an absolute path, but was '{directory}'.",
                nameof(directory));
        }

        return new ScriptEnvironment(
            namespaceNames,
            references,
            globalType,
            Path.GetFullPath(directory));
    }

    /// <summary>
    /// Returns a new instance with the specified globals type.
    /// </summary>
    /// <param name="globalType">The global type to set.</param>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithGlobalType(Type globalType)
    {
        if (globalType == null)
        {
            throw new ArgumentNullException(nameof(globalType));
        }

        return new ScriptEnvironment(
            namespaceNames,
            references,
            globalType,
            baseDirectory);
    }

    /// <summary>
    /// Returns a new instance with <typeparamref name="T"/> configured as the globals type.
    /// </summary>
    /// <typeparam name="T">The globals type exposed to the script.</typeparam>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithGlobalType<T>()
    {
        return new ScriptEnvironment(
            namespaceNames,
            references,
            typeof(T),
            baseDirectory);
    }

    /// <summary>
    /// Returns a new instance with the assembly containing <typeparamref name="T"/> added as a reference.
    /// </summary>
    /// <typeparam name="T">The type whose assembly reference will be added.</typeparam>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithAdditionalReferenceForType<T>()
        => WithAdditionalReferenceForType(typeof(T));

    /// <summary>
    /// Returns a new instance with the assembly containing <paramref name="type"/> added as a reference.
    /// </summary>
    /// <param name="type">The type whose assembly reference will be added.</param>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    /// <remarks>
    /// The non-generic form also accepts static types such as <see cref="System.Linq.Enumerable"/>,
    /// which cannot be used as generic type arguments.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    public ScriptEnvironment WithAdditionalReferenceForType(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return new ScriptEnvironment(
            namespaceNames,
            references.Add(type.Assembly),
            globalType,
            baseDirectory);
    }

    /// <summary>
    /// Returns a new instance with the namespace containing <typeparamref name="T"/> added to the imports.
    /// </summary>
    /// <typeparam name="T">The type whose namespace will be added.</typeparam>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="T"/> does not belong to a namespace.</exception>
    public ScriptEnvironment WithAdditionalNamespaceForType<T>()
    {
        var namespaceName = typeof(T).Namespace;
        if (namespaceName == null)
            throw new InvalidOperationException($"The namespace for type {typeof(T).Name} is null.");
        return WithAdditionalNamespaceName(namespaceName);
    }

    /// <summary>
    /// Loads an assembly by simple name or full display name.
    /// </summary>
    /// <remarks>
    /// On .NET Framework <see cref="Assembly.Load(string)"/> rejects a simple name for a
    /// strongly-named GAC assembly such as <c>"System.Core"</c>, so an assembly already present in
    /// the AppDomain is preferred. That lookup is skipped for full display names, where the caller
    /// has asked for a specific version and matching on the simple name could pick the wrong one.
    /// </remarks>
    private static Assembly LoadAssembly(string referenceName)
    {
        var isSimpleName = referenceName.IndexOf(',') < 0;

        if (isSimpleName)
        {
            var loaded = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => string.Equals(
                    a.GetName().Name,
                    referenceName,
                    StringComparison.OrdinalIgnoreCase));

            if (loaded != null)
            {
                return loaded;
            }
        }

        try
        {
            return Assembly.Load(referenceName);
        }
        catch (Exception ex) when (
            ex is FileNotFoundException ||
            ex is FileLoadException ||
            ex is BadImageFormatException)
        {
            throw new ArgumentException(
                $"Could not load an assembly named '{referenceName}'. Pass the full display name, " +
                $"or use {nameof(WithAdditionalReferenceForType)} to reference it by type.",
                nameof(referenceName),
                ex);
        }
    }
}
