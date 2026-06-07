using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;

namespace CDS.CSharpScript2;

/// <summary>
/// Provides methods for compiling C# scripts into <see cref="CompiledScript"/> instances.
/// </summary>
internal static class ScriptCompiler
{
    /// <summary>
    /// Compile a C# script that doesn't return any data.
    /// Default namespaces and assembly references are used (see <see cref="Defaults.TypesForNamespacesAndAssemblies"/>).
    /// Global variables are not used.
    /// </summary>
    /// <param name="script">Script text to compile</param>
    /// <returns>A compiled script</returns>
    public static CompiledScript Compile(string script) =>
        Compile<object>(script: script, typeOfGlobals: null);


    /// <summary>
    /// Compile a C# script that returns a specific type. 
    /// Default namespaces and assembly references are used (see <see cref="Defaults.TypesForNamespacesAndAssemblies"/>).
    /// Global variables are not used.
    /// </summary>
    /// <param name="script">Script text to compile</param>
    /// <typeparam name="TReturn">The type of object returned from the script.</typeparam>
    /// <returns>A compiled script.</returns>
    public static CompiledScript Compile<TReturn>(string script) =>
        Compile<TReturn>(script: script, typeOfGlobals: null);


    /// <summary>
    /// Compile a C# script that returns a specific type. 
    /// Default namespaces and assembly references are used (see <see cref="Defaults.TypesForNamespacesAndAssemblies"/>).
    /// </summary>
    /// <param name="script">Script text to compile</param>
    /// <param name="typeOfGlobals">Type of the Globals class used to provide global params to the script; null if not required.</param>
    /// <typeparam name="TReturn">The type of object returned from the script.</typeparam>
    /// <returns>A compiled script.</returns>
    public static CompiledScript Compile<TReturn>(
        string script,
        Type? typeOfGlobals) =>
        Compile<TReturn>(
            script: script,
            namespaceTypes: Defaults.TypesForNamespacesAndAssemblies,
            referenceTypes: Defaults.TypesForNamespacesAndAssemblies,
            typeOfGlobals: typeOfGlobals);

    /// <summary>
    /// Compile a C# script that returns a specific type. 
    /// </summary>
    /// <param name="script">Script text to compile.</param>
    /// <param name="namespaceTypes">Types whose namespaces are imported into the script.</param>
    /// <param name="referenceTypes">Types whose assemblies are referenced by the script.</param>
    /// <param name="typeOfGlobals">Type of the globals class used to provide global parameters to the script; <see langword="null"/> if not required.</param>
    /// <typeparam name="TReturn">The type of object returned from the script.</typeparam>
    /// <returns>A compiled script.</returns>
    public static CompiledScript Compile<TReturn>(
        string script,
        Type[]? namespaceTypes,
        Type[]? referenceTypes,
        Type? typeOfGlobals)
    {
        var namespaces = namespaceTypes?
            .Where(t => t.Namespace != null)
            .Select(t => t.Namespace!) ?? Enumerable.Empty<string>();

        var references = referenceTypes?
            .Select(t => t.Assembly) ?? Enumerable.Empty<Assembly>();

        return Compile<TReturn>(
            script: script,
            namespaces: namespaces,
            references: references,
            typeOfGlobals: typeOfGlobals);
    }


    /// <summary>
    /// Compile a C# script that returns a specific type. 
    /// </summary>
    /// <param name="script">Script text to compile.</param>
    /// <param name="namespaces">Namespace strings to import (e.g. <c>"System.Math"</c>).</param>
    /// <param name="references">Assemblies to reference within the script.</param>
    /// <param name="typeOfGlobals">Type of the globals class used to provide global parameters to the script; <see langword="null"/> if not required.</param>
    /// <typeparam name="TReturn">The type of object returned from the script.</typeparam>
    /// <returns>A compiled script.</returns>
    public static CompiledScript Compile<TReturn>(
        string script,
        IEnumerable<string> namespaces,
        IEnumerable<Assembly> references,
        Type? typeOfGlobals)
    {
        var scriptOptions = ScriptOptions.Default
            .WithImports(namespaces.Distinct())
            .AddReferences(references.Distinct());

        var compiledScript = CSharpScript.Create<TReturn>(
            script,
            globalsType: typeOfGlobals,
            options: scriptOptions);

        compiledScript.Compile();
        var compilation = compiledScript.GetCompilation();
        var diagnostics = compilation.GetDiagnostics();

        // get the syntax tree and semantic model
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var compilationWrapper = new CompiledScript(
            compiledScript,
            syntaxTree,
            semanticModel,
            diagnostics);

        return compilationWrapper;
    }
}
