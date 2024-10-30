using CDS.CSScripting;
using CDS.CSScripting.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public partial class ScriptManager
{
    private Document document;
    private string scriptText;
    private SyntaxTree cachedSyntaxTree;
    private SemanticModel cachedSemanticModel;
    private Compilation compilation;
    private ImmutableArray<Diagnostic> diagnostics;
    private SourceText scriptSourceText;
    private CDS.CSharpScripting.CompiledScript compiledScript;
    private CodeCompletionManager completionManager;


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


    private ScriptManager()
    {
        scriptText = "";

        // Create documentation providers
        MetadataReference metadataRefMSCorLib = GetMetadataReference(typeof(object));
        MetadataReference metadataRefConsoleDocumentation = GetMetadataReference(typeof(Console));

        // Create metadata references with documentation providers
        var references = new List<MetadataReference>
        {
            metadataRefMSCorLib,
            metadataRefConsoleDocumentation,
        };


        var compilationOptions = new CSharpCompilationOptions(
           OutputKind.DynamicallyLinkedLibrary,
           usings: new[] { "System" });

        var scriptProjectInfo =
            ProjectInfo
            .Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                name: "Script",
                assemblyName: "Script",
                LanguageNames.CSharp,
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

    private ScriptManager(Document scriptDocument, string scriptText)
    {
        this.document = scriptDocument;
        this.scriptText = scriptText;
    }

    public ScriptManager ApplyScript(string script)
    {
        var newSourceText = SourceText.From(script);
        var updatedDocument = document.WithText(newSourceText);
        return new ScriptManager(updatedDocument, script);
    }


    public async Task<ScriptManager> ApplyScriptAsync(string script)
    {
        return await Task.FromResult(ApplyScript(script));
    }


    public static async Task<ScriptManager> CreateAsync()
    {
        var task = Task.Run(() => new ScriptManager());
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


    public async Task Compile()
    {
        if (compiledScript != null) { return; }

        SourceText scriptSourceText = await GetScriptSourceTextAsync();
        string script = scriptSourceText.ToString();
        compiledScript = CDS.CSharpScripting.ScriptCompiler.Compile(script);
    }


    public async Task<CDS.CSharpScripting.CompilationOutput> GetCompilationOutput()
    {
        await Compile();
        return compiledScript.CompilationOutput;
    }


    public async Task<T> RunAsync<T>()
    {
        await Compile();
        return await CDS.CSharpScripting.ScriptRunner.RunAsync<T>(compiledScript, globals: null);
    }



    public async Task<ImmutableArray<CompletionItem>> GetCompletionSuggestionsAsync(int position)
    {
        completionManager = 
            completionManager ?? 
            new CodeCompletionManager(
                scriptText: scriptText, 
                document: document);

        return await 
            completionManager
            .GetCompletionSuggestionsAsync(position);
    }




    //public async Task GetMethodOverloadsAsync(int position)
    //{
    //    // Step 1: Get the Semantic Model from the Document
    //    var semanticModel = await document.GetSemanticModelAsync();

    //    // Step 2: Get the Syntax Root (entire syntax tree)
    //    var syntaxRoot = await document.GetSyntaxRootAsync();

    //    // Step 3: Find the nearest token to the given position
    //    var token = syntaxRoot.FindToken(position);

    //    // Step 4: Get the closest ancestor that is an InvocationExpressionSyntax (method invocation)
    //    var invocation = token.Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();

    //    if (invocation == null)
    //    {
    //        Console.WriteLine("No invocation found at the given position.");
    //        return;
    //    }

    //    // Step 5: Try to get the symbol info using speculative analysis if the code is incomplete
    //    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

    //    if (symbolInfo.Symbol == null)
    //    {
    //        // If we couldn't get the symbol, try speculative semantic analysis
    //        symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position, invocation, SpeculativeBindingOption.BindAsExpression);
    //    }

    //    var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

    //    if (methodSymbol == null)
    //    {
    //        Console.WriteLine("No method symbol found.");
    //        return;
    //    }

    //    // Step 6: Retrieve method overloads using LookupSymbols
    //    var methodGroup = semanticModel.LookupSymbols(position, methodSymbol.ContainingType, methodSymbol.Name)
    //                                   .OfType<IMethodSymbol>();

    //    // Step 7: Print method overloads and parameters
    //    foreach (var overload in methodGroup)
    //    {
    //        var parameters = overload.Parameters.Select(p => $"{p.Type} {p.Name}");
    //        Console.WriteLine($"{overload.Name}({string.Join(", ", parameters)})");
    //    }
    //}


    //public async Task<ImmutableArray<CompletionItem>> GetCompletionSuggestionsAsync(int position)
    //{
    //    CreateCompletionService();

    //    CompletionList completionList = await completionService.GetCompletionsAsync(
    //        scriptDocument,
    //        position,
    //        cancellationToken: default);

    //    List<CompletionItem> recommendedCompletionItems = new List<CompletionItem>();

    //    var completionItems = completionList.ItemsList;
    //    if (completionItems.Count > 0)
    //    {
    //        var firstCompletionItem = completionItems[0];
    //        int firstSpanLength = firstCompletionItem.Span.Length;

    //        CompletionMode mode;
    //        if (firstSpanLength == 0)
    //        {
    //            mode = CompletionMode.AllInAlphabeticalOrder;
    //        }
    //        else if (firstSpanLength == 1)
    //        {
    //            mode = CompletionMode.AllWithSingleLetterMatch;
    //        }
    //        else
    //        {
    //            mode = CompletionMode.MatchingFirstTwoOrMoreOnly;
    //        }

    //        string spanText =
    //            mode == CompletionMode.AllInAlphabeticalOrder ?
    //            "" :
    //            scriptText.Substring(firstCompletionItem.Span.Start, firstCompletionItem.Span.Length);

    //        foreach (CompletionItem completionItem in completionItems)
    //        {
    //            bool recommended = true;

    //            if (mode == CompletionMode.MatchingFirstTwoOrMoreOnly)
    //            {
    //                recommended =
    //                    completionItem
    //                    .DisplayText
    //                    .StartsWith(spanText, comparisonType: StringComparison.OrdinalIgnoreCase);
    //            }

    //            if (recommended)
    //            {
    //                recommendedCompletionItems.Add(completionItem);
    //            }
    //        }

    //        if (mode == CompletionMode.AllWithSingleLetterMatch)
    //        {
    //            CompletionItemSingleLetterMatchSorter sorter = new CompletionItemSingleLetterMatchSorter(spanText[0]);
    //            recommendedCompletionItems.Sort(sorter.Compare);
    //        }
    //        else
    //        {
    //            recommendedCompletionItems.Sort();
    //        }
    //    }

    //    return recommendedCompletionItems.ToImmutableArray();
    //}









    //public async Task Test(int position)
    //{
    //    SyntaxTree syntaxTree = await GetSyntaxTreeAsync();

    //    var compilation = CSharpCompilation.Create("ScriptAnalysis")
    //        .AddReferences(
    //            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    //            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
    //        .AddSyntaxTrees(syntaxTree);

    //    SemanticModel semanticModel = await GetSemanticModelAsync();

    //    SyntaxNode root = syntaxTree.GetRoot();
    //    SyntaxToken token = root.FindToken(position);
    //    SyntaxNode node = token.Parent;

    //    while (node != null && !(node is InvocationExpressionSyntax))
    //    {
    //        node = node.Parent;
    //    }
    //}

    public async Task<(DetailedTypeInfo typeInfo, IEnumerable<MethodOverloadInfo> memberInfos)> GetSuggestionsAsync(int position)
    {
        //X2.Test(await GetSyntaxTreeAsync(), await GetSemanticModelAsync(), position);

        var xmlInfo = XMLHelpManager.Test(
            syntaxTree: await GetSyntaxTreeAsync(),
            semanticModel: await GetSemanticModelAsync(),
            position: position);


        return xmlInfo;
    }


    private MetadataReference GetMetadataReference(Type type)
    {
        var assemblyPath = type.Assembly.Location;
        string xmlPath = GetXmlDocumentationPath(assemblyPath);
        
        DocumentationProvider documentationProvider =
            File.Exists(xmlPath) ?
            XmlDocumentationProvider.CreateFromFile(xmlPath) :
            DocumentationProvider.Default;

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
