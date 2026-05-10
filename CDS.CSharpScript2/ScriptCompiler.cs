using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;

namespace CDS.CSharpScript2;

/// <summary>
/// Script compilationn support
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
    public static CompiledScript Compile(string script)
    {
        return Compile<object>(
            script: script,
            typeOfGlobals: null);
    }


    /// <summary>
    /// Compile a C# script that returns a specific type. 
    /// Default namespaces and assembly references are used (see <see cref="Defaults.TypesForNamespacesAndAssemblies"/>).
    /// Global variables are not used.
    /// </summary>
    /// <param name="script">Script text to compile</param>
    /// <typeparam name="ReturnType">The type of object that is returned from the script</typeparam>
    /// <returns>A compiled script</returns>
    public static CompiledScript Compile<ReturnType>(string script)
    {
        return Compile<ReturnType>(
            script: script,
            typeOfGlobals: null);
    }


    /// <summary>
    /// Compile a C# script that returns a specific type. 
    /// Default namespaces and assembly references are used (see <see cref="Defaults.TypesForNamespacesAndAssemblies"/>).
    /// </summary>
    /// <param name="script">Script text to compile</param>
    /// <param name="typeOfGlobals">Type of the Globals class used to provide global params to the script; null if not required.</param>
    /// <typeparam name="ReturnType">The type of object that is returned from the script</typeparam>
    /// <returns>A compiled script</returns>
    public static CompiledScript Compile<ReturnType>(
        string script,
        Type? typeOfGlobals)
    {
        return Compile<ReturnType>(
            script: script,
            namespaceTypes: Defaults.TypesForNamespacesAndAssemblies,
            referenceTypes: Defaults.TypesForNamespacesAndAssemblies,
            typeOfGlobals: typeOfGlobals);
    }

    /// <summary>
    /// Compile a C# script that returns a specific type. 
    /// </summary>
    /// <param name="script">Script text to compile</param>
    /// <param name="namespaceTypes">An array of references. E.g. "System.Math"</param>
    /// <param name="referenceTypes">An array assemblies to reference; the assembly for each type in this array is loaded and made available to the script</param>
    /// <param name="typeOfGlobals">Type of the Globals class used to provide global params to the script; null if not required.</param>
    /// <typeparam name="ReturnType">The type of object that is returned from the script</typeparam>
    /// <returns>A compiled script</returns>
    public static CompiledScript Compile<ReturnType>(
        string script,
        Type[]? namespaceTypes,
        Type[]? referenceTypes,
        Type? typeOfGlobals)
    {
        List<string> namespaceTypesList = new List<string>();
        if(namespaceTypes != null)
        {
            foreach(var type in namespaceTypes)
            {
                if(type.Namespace != null)
                {
                    namespaceTypesList.Add(type.Namespace);
                }
            }
        }

        List<Assembly> referenceTypesList = new List<Assembly>();
        if (referenceTypes != null)
        {
            foreach (var type in referenceTypes)
            {
                if (type.Assembly != null)
                {
                    referenceTypesList.Add(type.Assembly);
                }
            }
        }

        return Compile<ReturnType>(
            script: script,
            namespaces: namespaceTypesList,
            references: referenceTypesList,
            typeOfGlobals: typeOfGlobals);
    }


    /// <summary>
    /// Compile a C# script that returns a specific type. 
    /// </summary>
    /// <param name="script">Script text to compile</param>
    /// <param name="namespaceTypes">An array of references. E.g. "System.Math"</param>
    /// <param name="referenceTypes">An array assemblies to reference; the assembly for each type in this array is loaded and made available to the script</param>
    /// <param name="typeOfGlobals">Type of the Globals class used to provide global params to the script; null if not required.</param>
    /// <typeparam name="ReturnType">The type of object that is returned from the script</typeparam>
    /// <returns>A compiled script</returns>
    public static CompiledScript Compile<ReturnType>(
        string script,
        IEnumerable<string> namespaces,
        IEnumerable<Assembly> references,
        Type? typeOfGlobals)
    {
        var scriptOptions = ScriptOptions.Default.WithImports(namespaces.Distinct());
        scriptOptions = scriptOptions.AddReferences(references.Distinct());

        var compiledScript = CSharpScript.Create<ReturnType>(
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
