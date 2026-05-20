using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;

namespace CDS.CSharpScript2;

/// <summary>
/// Immutable context pairing a script text with a configured Roslyn workspace document.
/// Create via <see cref="CreateAsync()"/>, then update text via <see cref="ApplyScript"/>.
/// Use <see cref="ScriptAnalyser"/> for editor feedback and <see cref="ScriptExecutor"/> to compile for execution.
/// </summary>
/// <remarks>
/// Only the instance returned by <see cref="CreateAsync()"/> owns the underlying
/// <see cref="Microsoft.CodeAnalysis.Workspace"/> and must be disposed when no longer needed.
/// Instances produced by <see cref="ApplyScript"/> share the same workspace and must not be disposed.
/// </remarks>
public class ScriptContext : IDisposable
{
    private readonly bool _ownsWorkspace;
    private bool _disposed;

    internal Document Document { get; }
    internal ScriptEnvironment Environment { get; }

    /// <summary>Gets the current script text.</summary>
    public string ScriptText { get; }

    private ScriptContext(Document document, string scriptText, ScriptEnvironment environment, bool ownsWorkspace)
    {
        Document = document;
        ScriptText = scriptText;
        Environment = environment;
        _ownsWorkspace = ownsWorkspace;
    }

    /// <summary>Creates a context using the default script environment.</summary>
    public static Task<ScriptContext> CreateAsync() => CreateAsync(ScriptEnvironment.Default);

    /// <summary>Creates a context using the supplied environment.</summary>
    public static async Task<ScriptContext> CreateAsync(ScriptEnvironment environment)
        => await Task.Run(() => CreateCore(environment)).ConfigureAwait(false);

    /// <summary>
    /// Returns a new context with the given script text applied.
    /// The workspace document is updated in-place; no compilation occurs.
    /// </summary>
    /// <remarks>
    /// The returned context does not own the workspace. Only dispose the original
    /// context returned by <see cref="CreateAsync()"/>.
    /// </remarks>
    public ScriptContext ApplyScript(string script)
    {
        var updatedDocument = Document.WithText(SourceText.From(script));
        return new ScriptContext(updatedDocument, script, Environment, ownsWorkspace: false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_ownsWorkspace)
            Document.Project.Solution.Workspace.Dispose();
    }

    private static ScriptContext CreateCore(ScriptEnvironment environment)
    {
        var references = new List<MetadataReference>();

        foreach (var assembly in environment.References)
            references.Add(GetMetadataReference(assembly));

        if (environment.GlobalType != null)
            references.Add(GetMetadataReference(environment.GlobalType));

        if (!ScriptEnvironment.IsNetFramework)
        {
            references.Add(GetMetadataReferenceForAssemblyName("System.Runtime"));
            references.Add(GetMetadataReferenceForAssemblyName("System.Collections"));
        }

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            usings: environment.NamespaceNames);

        var projectInfo = ProjectInfo
            .Create(
                id: ProjectId.CreateNewId(),
                version: VersionStamp.Create(),
                name: "Script",
                assemblyName: "Script",
                language: LanguageNames.CSharp,
                hostObjectType: environment.GlobalType,
                isSubmission: true)
            .WithMetadataReferences(references)
            .WithCompilationOptions(compilationOptions);

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(projectInfo);

        var documentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(project.Id),
            "Script",
            sourceCodeKind: SourceCodeKind.Script,
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(""), VersionStamp.Create())));

        var document = workspace.AddDocument(documentInfo);

        return new ScriptContext(document, "", environment, ownsWorkspace: true);
    }

    private static MetadataReference GetMetadataReference(Type type)
        => GetMetadataReference(type.Assembly);

    private static MetadataReference GetMetadataReference(Assembly assembly)
    {
        string xmlPath = GetXmlDocumentationPath(assembly.Location);
        var provider = XmlDocumentationProvider.CreateFromFile(xmlPath);
        return MetadataReference.CreateFromFile(assembly.Location, documentation: provider);
    }

    private static MetadataReference GetMetadataReferenceForAssemblyName(string assemblyName)
    {
        string xmlPath = TryFindXml($"{assemblyName}.xml") ?? string.Empty;
        string assemblyPath = string.IsNullOrEmpty(xmlPath)
            ? string.Empty
            : Path.Combine(Path.GetDirectoryName(xmlPath) ?? string.Empty, $"{assemblyName}.dll");

        if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
        {
            assemblyPath = typeof(object).Assembly.Location;
            xmlPath = GetXmlDocumentationPath(assemblyPath);
        }

        var provider = XmlDocumentationProvider.CreateFromFile(xmlPath);
        return MetadataReference.CreateFromFile(assemblyPath, documentation: provider);
    }

    private static string GetXmlDocumentationPath(string assemblyPath)
    {
        string xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        if (File.Exists(xmlPath)) return xmlPath;

        string xmlFileName = $"{Path.GetFileNameWithoutExtension(assemblyPath)}.xml";
        return TryFindXml(xmlFileName) ?? xmlPath;
    }

    private static string? TryFindXml(string xmlFileName)
    {
        var programFilesPaths = new[]
        {
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles),
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86)
        };

        foreach (var root in programFilesPaths)
        {
            var packsRoot = Path.Combine(root, "dotnet", "packs", "Microsoft.NETCore.App.Ref");
            if (!Directory.Exists(packsRoot)) continue;

            foreach (var packDir in Directory.GetDirectories(packsRoot)
                .OrderByDescending(d =>
                {
                    var name = Path.GetFileName(d);
                    return Version.TryParse(name, out var v) ? v : new Version(0, 0);
                }))
            {
                var refFolder = Path.Combine(packDir, "ref");
                if (!Directory.Exists(refFolder)) continue;

                foreach (var tfmDir in Directory.GetDirectories(refFolder).Where(f => Path.GetFileNameWithoutExtension(f).StartsWith("net")))
                {
                    var candidate = Path.Combine(tfmDir, xmlFileName);
                    if (File.Exists(candidate)) return candidate;
                }
            }
        }

        return null;
    }
}
