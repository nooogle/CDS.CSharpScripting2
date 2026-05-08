using System.Collections.Immutable;
using System.Reflection;

namespace CDS.CSharpScript2;

/// <summary>
/// Represents the environment configuration for scripting, including namespaces, references, and global variables.
/// </summary>
public class ScriptEnvironment
{
    private ImmutableList<string> namespaceNames;
    private ImmutableList<Assembly> references;
    private Type? globalType;
    private static ScriptEnvironment defaultInstance;
    private static readonly bool isNetFramework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Contains(".NET Framework");

    /// <summary>
    /// Gets the default script environment instance.
    /// </summary>
    public static ScriptEnvironment Default => defaultInstance;

    /// <summary>
    /// Gets a value indicating whether the current runtime is .NET Framework.
    /// </summary>
    public static bool IsNetFramework => isNetFramework;

    /// <summary>
    /// Gets the collection of namespace names.
    /// </summary>
    public IEnumerable<string> NamespaceNames => namespaceNames;

    /// <summary>
    /// Gets the collection of assembly references.
    /// </summary>
    public IEnumerable<Assembly> References => references;

    /// <summary>
    /// Gets the global type.
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
    /// Returns a new instance of <see cref="ScriptEnvironment"/> with an additional namespace type.
    /// </summary>
    /// <param name="type">The type whose namespace will be added.</param>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithAdditionalNamespaceType(Type type)
    {
        string typeNamespace = type.Namespace ?? throw new ArgumentException($"The type '{type.FullName}' does not have a namespace.", nameof(type));

        return new ScriptEnvironment(
            namespaceNames.Add(typeNamespace),
            references,
            globalType);
    }

    /// <summary>
    /// Returns a new instance of <see cref="ScriptEnvironment"/> with drawing references.
    /// </summary>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
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
    /// Returns a new instance of <see cref="ScriptEnvironment"/> with an additional namespace name.
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
    /// Returns a new instance of <see cref="ScriptEnvironment"/> with an additional reference name.
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
    /// Returns a new instance of <see cref="ScriptEnvironment"/> with a specified global type.
    /// </summary>
    /// <param name="globalType">The global type to set.</param>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithGlobalType(Type globalType)
    {
        return new ScriptEnvironment(
            namespaceNames,
            references,
            globalType);
    }

    /// <summary>
    /// Returns a new instance of <see cref="ScriptEnvironment"/> with a specified global type.
    /// </summary>
    /// <typeparam name="T">The global type to set.</typeparam>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithGlobalType<T>()
    {
        return new ScriptEnvironment(
            namespaceNames,
            references,
            typeof(T));
    }

    /// <summary>
    /// Returns a new instance of <see cref="ScriptEnvironment"/> with an additional reference for a specified type.
    /// </summary>
    /// <typeparam name="T">The type whose assembly reference will be added.</typeparam>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithAdditionalReferenceForType<T>()
    {
        var fullName = typeof(T).Assembly.FullName;
        if (fullName == null)
            throw new InvalidOperationException($"The assembly full name for type {typeof(T).Name} is null.");
        return WithAdditionalReferenceName(fullName);
    }

    /// <summary>
    /// Returns a new instance of <see cref="ScriptEnvironment"/> with an additional namespace for a specified type.
    /// </summary>
    /// <typeparam name="T">The type whose namespace will be added.</typeparam>
    /// <returns>A new instance of <see cref="ScriptEnvironment"/>.</returns>
    public ScriptEnvironment WithAdditionalNamespaceForType<T>()
    {
        var namespaceName = typeof(T).Namespace;
        if (namespaceName == null)
            throw new InvalidOperationException($"The namespace for type {typeof(T).Name} is null.");
        return WithAdditionalNamespaceName(namespaceName);
    }
}
