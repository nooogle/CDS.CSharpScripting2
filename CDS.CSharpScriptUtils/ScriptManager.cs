using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Reflection;

namespace CDS.CSharpScriptUtils
{
    public partial class ScriptManager
    {
        private Document document;
        private string scriptText;
        private ScriptEnvironment environment;
        private SourceText scriptSourceText;
        private CompiledScript compiledScript;


        public async Task<SyntaxTree> GetSyntaxTreeAsync()
        {
            return await document.GetSyntaxTreeAsync();

            //await CompileAsync();
            //return compiledScript.SyntaxTree;
        }


        public async Task<SemanticModel> GetSemanticModelAsync()
        {
            return await document.GetSemanticModelAsync();

            //await CompileAsync();
            //return compiledScript.SemanticModel;
        }



        public string ScriptText => scriptText;


        private ScriptManager(ScriptEnvironment environment)
        {
            scriptText = "";
            this.environment = environment;

            var references = new List<MetadataReference>();

            foreach (var reference in environment.References)
            {
                MetadataReference metadataRef = GetMetadataReference(reference);
                references.Add(metadataRef);
            }


            if (environment.GlobalType != null)
            {
                MetadataReference metadataRefGlobal = GetMetadataReference(environment.GlobalType);
                references.Add(metadataRefGlobal);
            }

            if (!ScriptEnvironment.IsNetFramework)
            {
                MetadataReference metadataRefRuntime = GetMetadataReferenceForRuntime();
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
                    isSubmission: false)
                .WithMetadataReferences(references)
                .WithCompilationOptions(compilationOptions);

            var workspace = new AdhocWorkspace();
            var scriptProject = workspace.AddProject(scriptProjectInfo);

            var scriptDocumentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(scriptProject.Id), "Script",
                sourceCodeKind: SourceCodeKind.Script,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(scriptText), VersionStamp.Create())));


            document = workspace.AddDocument(scriptDocumentInfo);

            //{
            //    var semanticModel = document.GetSemanticModelAsync().Result;
            //    var syntaxTree = document.GetSyntaxTreeAsync().Result;

            //    var root = syntaxTree.GetRoot();

            //    var identifier = root
            //        .DescendantNodes()
            //        .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax>()
            //        .First(id => id.Identifier.Text == "Pow");

            //    var symbolInfo = semanticModel.GetSymbolInfo(identifier);
            //    var symbol = symbolInfo.Symbol;
            //    string xmlDocumentation = symbol.GetDocumentationCommentXml() ?? string.Empty;

            //    System.Diagnostics.Debug.WriteLine("XML Documentation: " + xmlDocumentation);
            //}

            //{
            //    var semanticModel2 = GetSemanticModelAsync().Result;
            //    var syntaxTree2 = GetSyntaxTreeAsync().Result;

            //    var root = syntaxTree2.GetRoot();

            //    var identifier = root
            //        .DescendantNodes()
            //        .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax>()
            //        .First(id => id.Identifier.Text == "Pow");

            //    var symbolInfo = semanticModel2.GetSymbolInfo(identifier);
            //    var symbol = symbolInfo.Symbol;
            //    string xmlDocumentation = symbol.GetDocumentationCommentXml() ?? string.Empty;

            //    System.Diagnostics.Debug.WriteLine("XML Documentation: " + xmlDocumentation);
            //}
        }

        private ScriptManager(Document scriptDocument, string scriptText, ScriptEnvironment environment)
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


        public static async Task<ScriptManager> CreateAsync() => await CreateAsync(ScriptEnvironment.Default);


        public static async Task<ScriptManager> CreateAsync(ScriptEnvironment environment)
        {
            var task = Task.Run(() => new ScriptManager(environment));
            return await task;
        }


        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync()
        {
            await CompileAsync();
            return compiledScript.CompilationOutput.Diagnostics;
        }



        /// <summary>
        /// Get the source text asynchronously
        /// </summary>
        public async Task<SourceText> GetScriptSourceTextAsync() => scriptSourceText ??= await document.GetTextAsync();


        /// <summary>
        /// Compiles the script
        /// </summary>
        public async Task CompileAsync()
        {
            if (compiledScript != null) { return; }

            SourceText scriptSourceText = await GetScriptSourceTextAsync();
            string script = scriptSourceText.ToString();

            compiledScript = ScriptCompiler.Compile<object>(
                script,
                namespaces: environment.NamespaceNames,
                references: environment.References,
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
            return await CodeCompletion.Manager.Get(
                    scriptText: scriptText,
                    document: document,
                    cursorPosition: position);
        }


        public async Task<APIInfo.APIInfoResult> GetSuggestionsAsync(int position)
        {
            var xmlInfo = APIInfo.APIInfoService.Get(
                syntaxTree: await GetSyntaxTreeAsync(),
                semanticModel: await GetSemanticModelAsync(),
                position: position);


            return xmlInfo;
        }

        private static MetadataReference GetMetadataReference(Type type)
        {
            return GetMetadataReference(type.Assembly);
        }

        private static MetadataReference GetMetadataReference(Assembly assembly)
        {
            string xmlPath = GetXmlDocumentationPath(assembly.Location);

            DocumentationProvider documentationProvider = XmlDocumentationProvider.CreateFromFile(xmlPath);

            var metadataReference = MetadataReference.CreateFromFile(
                path: assembly.Location,
                documentation: documentationProvider);

            return metadataReference;
        }

        private static MetadataReference GetMetadataReferenceForRuntime()
        {
            string xmlPath = TryFindXMLForXXX("System.Runtime.xml");
            string assemblyPath = Path.Combine(Path.GetDirectoryName(xmlPath), "System.Runtime.dll");

            DocumentationProvider documentationProvider = XmlDocumentationProvider.CreateFromFile(xmlPath);

            var metadataReference = MetadataReference.CreateFromFile(
                path: assemblyPath,
                documentation: documentationProvider);

            return metadataReference;
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

            var xxx = TryFindXMLForXXX(xmlFileName);

            return xxx ?? xmlPath;
        }


        private static string? TryFindXMLForXXX(string xmlFileName)
        {
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
            }


            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }
    }
}
