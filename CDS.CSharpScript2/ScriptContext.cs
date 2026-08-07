using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

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

        if (ScriptEnvironment.IsNetFramework)
        {
            // netstandard.dll is required so Roslyn can follow type-forwards from assemblies
            // that target .NET Standard (e.g. OpenCvSharp) back to mscorlib.dll.
            // Without it the workspace compilation reports CS0012 on types like System.Object.
            var netStandardRef = TryGetNetStandardReference();
            if (netStandardRef != null)
                references.Add(netStandardRef);
        }
        else
        {
            // Gives a minimal environment (nothing but ScriptEnvironment.Default) enough of the BCL
            // to compile ordinary code — corlib itself, not the System.Runtime/System.Collections
            // reference-assembly facades from the Microsoft.NETCore.App.Ref pack this used to add.
            // Those facades redeclare the whole BCL surface (Stopwatch included) as their own types
            // rather than forwarding to CoreLib, so they collided with CoreLib itself the moment
            // anything else in the compilation also referenced it — which a #r directive resolved via
            // environment.MetadataResolver does for whatever it references, producing CS0433 on the
            // editor's diagnostics path only, for any script that both uses #r and touches a type the
            // facade duplicated. Referencing CoreLib directly means there is only ever one definition
            // of any BCL type in play, so a #r-triggered CoreLib reference just coincides with this
            // one instead of competing with it.
            references.Add(GetCoreLibMetadataReference());
        }

        // Resolvers come from the environment so the editor and the execution path accept exactly
        // the same #r and #load directives. Without them the workspace compilation rejects #r with
        // CS7099 and #load with CS8099, squiggling directives that compile and run perfectly well.
        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            usings: environment.NamespaceNames,
            metadataReferenceResolver: environment.MetadataResolver,
            sourceReferenceResolver: environment.SourceResolver);

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

    private static readonly Lazy<byte[]?> s_coreLibDocumentationXml = new(BuildCoreLibDocumentationXml);

    /// <summary>
    /// References CoreLib itself, documented from the reference-assembly pack. CoreLib's own XML
    /// doc file doesn't exist — the SDK ships its documentation split across several differently
    /// named files (<c>System.Runtime.xml</c>, <c>System.Collections.xml</c>, ...), none of which
    /// matches CoreLib's own assembly name, so <see cref="GetXmlDocumentationPath"/>'s filename-based
    /// lookup finds nothing for it. Doc IDs are keyed by symbol rather than by source file, so
    /// concatenating the &lt;member&gt; entries from each pack file into one in-memory document works
    /// regardless of which physical assembly a symbol is now resolved from.
    /// </summary>
    private static MetadataReference GetCoreLibMetadataReference()
    {
        var assembly = typeof(object).Assembly;
        var provider = s_coreLibDocumentationXml.Value is { } xml
            ? XmlDocumentationProvider.CreateFromBytes(xml)
            : XmlDocumentationProvider.CreateFromFile(GetXmlDocumentationPath(assembly.Location));

        return MetadataReference.CreateFromFile(assembly.Location, documentation: provider);
    }

    private static byte[]? BuildCoreLibDocumentationXml()
    {
        var sourceFiles = new[] { "System.Runtime.xml", "System.Collections.xml" }
            .Select(TryFindXml)
            .Where(path => path != null)
            .Cast<string>()
            .ToList();

        if (sourceFiles.Count == 0)
        {
            return null;
        }

        var members = new XElement("members");

        foreach (var sourceFile in sourceFiles)
        {
            try
            {
                var sourceMembers = XDocument.Load(sourceFile).Root?.Element("members")?.Elements("member");
                if (sourceMembers != null)
                {
                    members.Add(sourceMembers);
                }
            }
            catch (System.Xml.XmlException)
            {
                // Skip a source file that fails to parse; the rest still contribute.
            }
        }

        var merged = new XDocument(
            new XElement("doc",
                new XElement("assembly", new XElement("name", "System.Private.CoreLib")),
                members));

        return Encoding.UTF8.GetBytes(merged.ToString());
    }

    private static string GetXmlDocumentationPath(string assemblyPath)
    {
        string xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        if (File.Exists(xmlPath)) return xmlPath;

        string xmlFileName = $"{Path.GetFileNameWithoutExtension(assemblyPath)}.xml";
        return TryFindXml(xmlFileName) ?? xmlPath;
    }

    private static MetadataReference? TryGetNetStandardReference()
    {
        // netstandard.dll lives in the same directory as mscorlib.dll on .NET Framework 4.x.
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (runtimeDir is null)
            return null;

        var path = Path.Combine(runtimeDir, "netstandard.dll");
        if (!File.Exists(path))
            return null;

        string xmlPath = GetXmlDocumentationPath(path);
        var provider = XmlDocumentationProvider.CreateFromFile(xmlPath);
        return MetadataReference.CreateFromFile(path, documentation: provider);
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
            // .NET 5+ reference assemblies
            var packsRoot = Path.Combine(root, "dotnet", "packs", "Microsoft.NETCore.App.Ref");
            if (Directory.Exists(packsRoot))
            {
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

            // .NET Framework reference assemblies (xml docs are not shipped next to runtime dlls)
            var netFxRoot = Path.Combine(root, "Reference Assemblies", "Microsoft", "Framework", ".NETFramework");
            if (Directory.Exists(netFxRoot))
            {
                foreach (var versionDir in Directory.GetDirectories(netFxRoot)
                    .OrderByDescending(d => d))
                {
                    var candidate = Path.Combine(versionDir, xmlFileName);
                    if (File.Exists(candidate)) return candidate;
                }
            }
        }

        return null;
    }
}
