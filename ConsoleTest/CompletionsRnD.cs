using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;

namespace ConsoleTest;

class CompletionsRnD
{
    public static async Task Run()
    {
        await ViaScriptManager();
        await ViaDocument();
        await ViaCompiliation();
    }

    private static async Task ViaScriptManager()
    {
        var env =
            CDS.CSharpScript2.ScriptEnvironment.Default;

        var scriptManager = await CDS.CSharpScript2.ScriptManager.CreateAsync(env);

        scriptManager = scriptManager.ApplyScript("System.Math.Pow(1, 2)");

        await scriptManager.CompileAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();

        var semanticModel = await scriptManager.GetSemanticModelAsync();
        var syntaxTree = await scriptManager.GetSyntaxTreeAsync();

        var root = syntaxTree.GetRoot();

        var identifier = root
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .First(id => id.Identifier.Text == "Pow");

        // Get the symbol information for the identifier.
        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
        var symbol = symbolInfo.Symbol;
        if (symbol == null)
        {
            Console.WriteLine("Symbol not found!");
            return;
        }

        // Display the symbol's full display string (i.e. its type and namespace info).
        Console.WriteLine("Symbol: " + symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));

        // Retrieve the XML documentation for the symbol.
        // This includes the summary and any additional XML comments that were defined.
        string xmlDocumentation = symbol.GetDocumentationCommentXml() ?? string.Empty;

        // Output the raw XML documentation.
        Console.WriteLine("\nDocumentation XML:");
        Console.WriteLine(xmlDocumentation);
    }

    private static async Task ViaDocument()
    {
        var references = new[] { GetMetadataReferenceForRuntime() }; //, GetMetadataReference(typeof(Console)) };

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            usings: new[] { "System" });


        var scriptProjectInfo =
            ProjectInfo
            .Create(
                id: ProjectId.CreateNewId(),
                version: VersionStamp.Create(),
                name: "Script",
                assemblyName: "Script",
                language: LanguageNames.CSharp,
                hostObjectType: null,
                isSubmission: false)
            .WithMetadataReferences(references)
            .WithCompilationOptions(compilationOptions);

        var workspace = new AdhocWorkspace();
        var scriptProject = workspace.AddProject(scriptProjectInfo);

        var scriptText = @"Math.Pow(1, 2)";

        var scriptDocumentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(scriptProject.Id), "Script",
            sourceCodeKind: SourceCodeKind.Script,
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(scriptText), VersionStamp.Create())));


        var document = workspace.AddDocument(scriptDocumentInfo);

        // get the semantic model
        var semanticModel = document.GetSemanticModelAsync().Result;
        var syntaxTree = document.GetSyntaxTreeAsync().Result;

        var root = syntaxTree.GetRoot();

        var identifier = root
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .First(id => id.Identifier.Text == "Pow");

        // Get the symbol information for the identifier.
        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
        var symbol = symbolInfo.Symbol;
        if (symbol == null)
        {
            Console.WriteLine("Symbol not found!");
            return;
        }

        // Display the symbol's full display string (i.e. its type and namespace info).
        Console.WriteLine("Symbol: " + symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));

        // Retrieve the XML documentation for the symbol.
        // This includes the summary and any additional XML comments that were defined.
        string xmlDocumentation = symbol.GetDocumentationCommentXml() ?? string.Empty;

        // Output the raw XML documentation.
        Console.WriteLine("\nDocumentation XML:");
        Console.WriteLine(xmlDocumentation);
    }

    private static async Task ViaCompiliation()
    {
        var mscorlib = GetMetadataReferenceForRuntime();
        var systemConsole = GetMetadataReference(typeof(Console));

        var compiliationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            usings: new[] { "System" });

        var code = @"System.Math.Pow(1, 2)";
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var compilation = CSharpCompilation.Create(
            assemblyName: "DemoAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: new[] { mscorlib, systemConsole },
            compiliationOptions);

        // Obtain the semantic model.
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var root = syntaxTree.GetRoot();

        var identifier = root
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .First(id => id.Identifier.Text == "Pow");

        // Get the symbol information for the identifier.
        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
        var symbol = symbolInfo.Symbol;
        if (symbol == null)
        {
            Console.WriteLine("Symbol not found!");
            return;
        }

        // Display the symbol's full display string (i.e. its type and namespace info).
        Console.WriteLine("Symbol: " + symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));

        // Retrieve the XML documentation for the symbol.
        // This includes the summary and any additional XML comments that were defined.
        string xmlDocumentation = symbol.GetDocumentationCommentXml() ?? string.Empty;

        // Output the raw XML documentation.
        Console.WriteLine("\nDocumentation XML:");
        Console.WriteLine(xmlDocumentation);
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
