using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CDS.CSScripting
{
    public partial class ScriptManager
    {
        private Document document;
        private string scriptText;
        private Env environment;
        private SyntaxTree cachedSyntaxTree;
        private SemanticModel cachedSemanticModel;
        private Compilation compilation;
        private ImmutableArray<Diagnostic> diagnostics;
        private SourceText scriptSourceText;
        private CompiledScript compiledScript;
        private CodeCompletion.Manager completionManager;


        public async Task<SyntaxTree> GetSyntaxTreeAsync()
        {
            if (cachedSyntaxTree == null)
            {
                cachedSyntaxTree = await document.GetSyntaxTreeAsync();
            }

            return cachedSyntaxTree;
        }


        public async Task<SemanticModel> GetSemanticModelAsync()
        {
            if (cachedSemanticModel == null)
            {
                cachedSemanticModel = await document.GetSemanticModelAsync();
            }

            return cachedSemanticModel;
        }



        public string ScriptText => scriptText;


        private ScriptManager(Env environment)
        {
            scriptText = "";
            this.environment = environment;

            // Create documentation providers
            MetadataReference metadataRefMSCorLib = GetMetadataReference(typeof(object));
            MetadataReference metadataRefConsoleDocumentation = GetMetadataReference(typeof(Console));
            
            // Create metadata references with documentation providers
            var references = new List<MetadataReference>
            {
                metadataRefMSCorLib,
                metadataRefConsoleDocumentation,
            };

            foreach (var referenceName in environment.ReferenceNames)
            {
                MetadataReference metadataRef = GetMetadataReference(referenceName);
                references.Add(metadataRef);
            }


            if (environment.GlobalType != null)
            {
                MetadataReference metadataRefGlobal = GetMetadataReference(environment.GlobalType);
                references.Add(metadataRefGlobal);
            }

            if (!Env.IsNetFramework)
            {
                MetadataReference metadataRefRuntime = MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location);

                references.Add(metadataRefRuntime);
            }


            var compilationOptions = new CSharpCompilationOptions(
               OutputKind.DynamicallyLinkedLibrary,
               usings: environment.NamespaceNames);

            var scriptProjectInfo =
                ProjectInfo
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
            var scriptProject = workspace.AddProject(scriptProjectInfo);


            var scriptDocumentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(scriptProject.Id), "Script",
                sourceCodeKind: SourceCodeKind.Script,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(scriptText), VersionStamp.Create())));


            document = workspace.AddDocument(scriptDocumentInfo);
        }

        private ScriptManager(Document scriptDocument, string scriptText, Env environment)
        {
            this.document = scriptDocument;
            this.scriptText = scriptText;
            this.environment = environment;
        }

        public ScriptManager ApplyScript(string script)
        {
            var newSourceText = SourceText.From(script);
            var updatedDocument = document.WithText(newSourceText);
            return new ScriptManager(updatedDocument, script, environment);
        }


        public async Task<ScriptManager> ApplyScriptAsync(string script)
        {
            return await Task.FromResult(ApplyScript(script));
        }


        public static async Task<ScriptManager> CreateAsync() => await CreateAsync(Env.Default);


        public static async Task<ScriptManager> CreateAsync(Env environment)
        {
            var task = Task.Run(() => new ScriptManager(environment));
            return await task;
        }


        private async Task<Compilation> GetCompilationAsync()
        {
            if (compilation != null) { return compilation; }

            compilation = await document.Project.GetCompilationAsync();
            return compilation;
        }


        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync()
        {
            if (diagnostics != null) { return diagnostics; }

            diagnostics = (await GetCompilationAsync()).GetDiagnostics();
            return diagnostics;
        }



        public async Task<SourceText> GetScriptSourceTextAsync() => scriptSourceText ?? (scriptSourceText = await document.GetTextAsync());


        public async Task CompileAsync()
        {
            if (compiledScript != null) { return; }

            SourceText scriptSourceText = await GetScriptSourceTextAsync();
            string script = scriptSourceText.ToString();
            
            compiledScript = ScriptCompiler.Compile<object>(
                script,
                namespaces: environment.NamespaceNames,
                references: environment.ReferenceNames,
                typeOfGlobals: environment.GlobalType);
        }


        public async Task<CompilationOutput> GetCompilationOutputAsync()
        {
            await CompileAsync();
            return compiledScript.CompilationOutput;
        }


        public async Task RunAsync()
        {
            await RunAsync<object>(globals: null);
        }


        public async Task<T> RunAsync<T>()
        {
            return await RunAsync<T>(globals: null);
        }

        public async Task RunAsync(object globals)
        {
            await RunAsync<object>(globals);
        }


        public async Task<T> RunAsync<T>(object globals)
        {
            await CompileAsync();
            return await ScriptRunner.RunAsync<T>(compiledScript, globals: globals);
        }


        public async Task<ImmutableArray<CompletionItem>> GetCompletionSuggestionsAsync(int position)
        {
            completionManager =
                completionManager ??
                new CodeCompletion.Manager(
                    scriptText: scriptText,
                    document: document);

            return await
                completionManager
                .GetCompletionSuggestionsAsync(position);
        }


        public async Task<(DetailedTypeInfo typeInfo, IEnumerable<MethodOverloadInfo> memberInfos)> GetSuggestionsAsync(int position)
        {
            var xmlInfo = XMLHelpManager.Test(
                syntaxTree: await GetSyntaxTreeAsync(),
                semanticModel: await GetSemanticModelAsync(),
                position: position);


            return xmlInfo;
        }
        
        private MetadataReference GetMetadataReference(string assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);
            string xmlPath = GetXmlDocumentationPath(assembly.Location);

            DocumentationProvider documentationProvider =
                File.Exists(xmlPath) ?
                XmlDocumentationProvider.CreateFromFile(xmlPath) :
                DocumentationProvider.Default;

            var metadataReference = MetadataReference.CreateFromFile(
                path: assembly.Location,
                documentation: documentationProvider);

            return metadataReference;
        }


        private MetadataReference GetMetadataReference(Type type)
        {
            var assemblyPath = type.Assembly.GetName().Name;
            return GetMetadataReference(assemblyPath);
        }


        private static string GetXmlDocumentationPath(string assemblyPath)
        {
            string xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            if (File.Exists(xmlPath))
            {
                return xmlPath;
            }

            // Fallback to common locations
            string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

            var xmlFileName = $"{assemblyName}.xml";

            var programFilesPaths = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            var possiblePaths = new List<string>();


            foreach (var programFilesPath in programFilesPaths)
            {
                // .NET Packs
                var packsRoot = Path.Combine(
                    programFilesPath,
                    "dotnet",
                    "packs",
                    "Microsoft.NETCore.App.Ref");

                if (Directory.Exists(packsRoot))
                {
                    var packsSubFolders = Directory.GetDirectories(packsRoot).OrderByDescending(d => d);

                    foreach (var packSubFolder in packsSubFolders)
                    {
                        var refFolder = Path.Combine(packSubFolder, "ref");
                        if (!Directory.Exists(refFolder))
                        {
                            continue;
                        }

                        foreach (var xmlFolder in Directory.GetDirectories(refFolder).Where(f => Path.GetFileNameWithoutExtension(f).StartsWith("net")))
                        {
                            possiblePaths.Add(Path.Combine(xmlFolder, xmlFileName));
                        }
                    }
                }


                // .NET Framework paths
                foreach (var netFrameworkVersion in new[] { "v4.8.1", "v4.8", "v4.7.2" })
                {
                    possiblePaths.Add(Path.Combine(programFilesPath, "Reference Assemblies", "Microsoft", "Framework", ".NETFramework", netFrameworkVersion, $"{assemblyName}.xml"));
                }
            }



            //foreach (var programFilesPath in programFilesPaths)
            //{
            //    // .NET Packs
            //    var packsRoot = Path.Combine(
            //        programFilesPath,
            //        "dotnet",
            //        "packs",
            //        "Microsoft.NETCore.App.Ref");

            //    if (Directory.Exists(packsRoot))
            //    {
            //        var packsSubFolders = Directory.GetDirectories(packsRoot);

            //        foreach (var packSubFolder in packsSubFolders)
            //        {
            //            var refFolder = Path.Combine(packSubFolder, "ref");
            //            if (!Directory.Exists(refFolder))
            //            {
            //                continue;
            //            }

            //            foreach (var xmlFolder in Directory.GetDirectories(refFolder).Where(f => Path.GetFileNameWithoutExtension(f).StartsWith("net")))
            //            {
            //                possiblePaths.Add(Path.Combine(xmlFolder, xmlFileName));
            //            }
            //        }
            //    }

            //    possiblePaths.Add(Path.Combine(programFilesPath, "dotnet", "shared", "Microsoft.NETCore.App", "5.0.0", $"{assemblyName}.xml"));

            //    possiblePaths.Add(Path.Combine(programFilesPath, "dotnet", "shared", "Microsoft.NETCore.App", "8.0.10", $"{assemblyName}.xml"));

            //    // .NET Framework paths
            //    foreach (var netFrameworkVersion in new[] { "v4.8.1", "v4.8", "v4.7.2" })
            //    {
            //        possiblePaths.Add(Path.Combine(programFilesPath, "Reference Assemblies", "Microsoft", "Framework", ".NETFramework", netFrameworkVersion, $"{assemblyName}.xml"));
            //    }

            //    // Add more versions if needed
            //    foreach (var netCoreVersion in new[] { "3.1.0", "3.0.0", "2.2.0", "2.1.0" })
            //    {
            //        possiblePaths.Add(Path.Combine(programFilesPath, "dotnet", "packs", "Microsoft.NETCore.App.Ref", netCoreVersion, "ref", $"netcoreapp{netCoreVersion}", $"{assemblyName}.xml"));
            //    }
            //}

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return xmlPath; // Return the original path if no fallback found
        }
    }
}
