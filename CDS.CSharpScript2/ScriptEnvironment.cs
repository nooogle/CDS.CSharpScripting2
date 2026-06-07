using System.Collections.Immutable;
using System.Reflection;

namespace CDS.CSharpScript2;

/// <summary>
/// Represents the immutable configuration used when compiling and running scripts,
/// including imported namespaces, referenced assemblies, and the optional globals type.
/// </summary>
public class ScriptEnvironment
{
    private ImmutableList<string> namespaceNames;
    private ImmutableList<Assembly> references;
    private Type? globalType;
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
            globalType: null);
    }

    private ScriptEnvironment(ImmutableList<string> namespaceNames, ImmutableList<Assembly> references, Type? globalType)
    {
        this.namespaceNames = namespaceNames.Distinct().ToImmutableList();
        this.references = references.Distinct().ToImmutableList();
        this.globalType = globalType;
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
            globalType);
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
                globalType);
        }

        var newReferenceNames = new[] { "System.Drawing", "System.Drawing.Primitives" };
        var newAssemblies = newReferenceNames.Select(Assembly.Load);

        return new ScriptEnvironment(
            namespaceNames: namespaceNames,
            references: references.AddRange(newAssemblies),
            globalType);
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
            globalType);
    }

    /// <summary>
    /// Returns a new instance with an additional assembly reference loaded by display name.
    /// </summary>
    /// <param name="referenceName">The reference name to add.</param>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithAdditionalReferenceName(string referenceName)
    {
        var assembly = Assembly.Load(referenceName);

        return new ScriptEnvironment(
            namespaceNames,
            references.Add(assembly),
            globalType);
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
            globalType);
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
            typeof(T));
    }

    /// <summary>
    /// Returns a new instance with the assembly containing <typeparamref name="T"/> added as a reference.
    /// </summary>
    /// <typeparam name="T">The type whose assembly reference will be added.</typeparam>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithAdditionalReferenceForType<T>()
        => new ScriptEnvironment(namespaceNames, references.Add(typeof(T).Assembly), globalType);

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
}
