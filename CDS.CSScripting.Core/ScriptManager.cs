using ConsoleScratchFramework.ForLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

public class ScriptManager
{
    private Document document;
    private string scriptText;
    private SyntaxTree cachedSyntaxTree;
    private SemanticModel cachedSemanticModel;


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
        var workspace = new AdhocWorkspace();

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
                isSubmission: true);

        scriptProjectInfo =
            scriptProjectInfo
            .WithMetadataReferences(new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location)
            });

        scriptProjectInfo =
            scriptProjectInfo
            .WithCompilationOptions(compilationOptions);


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


    private Compilation compilation;
    private ImmutableArray<Diagnostic> diagnostics;


    public async Task<Compilation> GetCompilationAsync()
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


    SourceText scriptSourceText;
    CDS.CSharpScripting.CompiledScript compiledScript;


    public async Task<SourceText> GetScriptSourceTextAsync() => scriptSourceText ?? (scriptSourceText = await document.GetTextAsync());


    public void Compile()
    {
        if (compiledScript != null) { return; }

        SourceText scriptSourceText = GetScriptSourceTextAsync().Result;
        string script = scriptSourceText.ToString();
        compiledScript = CDS.CSharpScripting.ScriptCompiler.Compile(script);
    }


    public CDS.CSharpScripting.CompilationOutput GetCompilationOutput()
    {
        Compile();
        return compiledScript.CompilationOutput;
    }


    public async Task<T> RunAsync<T>()
    {
        Compile();
        return await CDS.CSharpScripting.ScriptRunner.RunAsync<T>(compiledScript, globals: null);
    }


    enum CompletionMode
    {
        AllInAlphabeticalOrder,
        AllWithSingleLetterMatch,
        MatchingFirstTwoOrMoreOnly,
    }


    private CompletionService completionService;


    /// <summary>
    /// Create a completion service if it does not already exist
    /// </summary>
    private void CreateCompletionService()
    {
        completionService = completionService ?? CompletionService.GetService(document);
    }


    public async Task<ImmutableArray<CompletionItem>> GetCompletionSuggestionsAsync(int position)
    {
        CreateCompletionService();

        var completionList = await GetCompletionListAsync(position);
        if (completionList == null || completionList.ItemsList.Count == 0)
        {
            return ImmutableArray<CompletionItem>.Empty;
        }

        var completionMode = DetermineCompletionMode(completionList.ItemsList[0]);
        var spanText = GetSpanText(completionMode, completionList.ItemsList[0]);

        var filteredItems = FilterCompletionItems(completionList.ItemsList.ToImmutableArray(), completionMode, spanText);
        SortCompletionItems(filteredItems, completionMode, spanText);

        return filteredItems.ToImmutableArray();
    }

    private async Task<CompletionList> GetCompletionListAsync(int position)
    {
        return await completionService.GetCompletionsAsync(document, position, cancellationToken: default);
    }

    private CompletionMode DetermineCompletionMode(CompletionItem firstItem)
    {
        int spanLength = firstItem.Span.Length;

        if (spanLength == 0)
        {
            return CompletionMode.AllInAlphabeticalOrder;
        }
        else if (spanLength == 1)
        {
            return CompletionMode.AllWithSingleLetterMatch;
        }
        else
        {
            return CompletionMode.MatchingFirstTwoOrMoreOnly;
        }
    }

    private string GetSpanText(CompletionMode mode, CompletionItem firstItem)
    {
        return mode == CompletionMode.AllInAlphabeticalOrder
            ? string.Empty
            : scriptText.Substring(firstItem.Span.Start, firstItem.Span.Length);
    }

    private List<CompletionItem> FilterCompletionItems(ImmutableArray<CompletionItem> items, CompletionMode mode, string spanText)
    {
        if (mode != CompletionMode.MatchingFirstTwoOrMoreOnly)
        {
            return items.ToList();
        }

        return items.Where(item => item.DisplayText.StartsWith(spanText, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void SortCompletionItems(List<CompletionItem> items, CompletionMode mode, string spanText)
    {
        if (mode == CompletionMode.AllWithSingleLetterMatch && !string.IsNullOrEmpty(spanText))
        {
            var sorter = new CompletionItemSingleLetterMatchSorter(spanText[0]);
            items.Sort(sorter.Compare);
        }
        else
        {
            items.Sort(); // Use default sort
        }
    }

    public async Task GetMethodOverloadsAsync(int position)
    {
        // Step 1: Get the Semantic Model from the Document
        var semanticModel = await document.GetSemanticModelAsync();

        // Step 2: Get the Syntax Root (entire syntax tree)
        var syntaxRoot = await document.GetSyntaxRootAsync();

        // Step 3: Find the nearest token to the given position
        var token = syntaxRoot.FindToken(position);

        // Step 4: Get the closest ancestor that is an InvocationExpressionSyntax (method invocation)
        var invocation = token.Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();

        if (invocation == null)
        {
            Console.WriteLine("No invocation found at the given position.");
            return;
        }

        // Step 5: Try to get the symbol info using speculative analysis if the code is incomplete
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

        if (symbolInfo.Symbol == null)
        {
            // If we couldn't get the symbol, try speculative semantic analysis
            symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position, invocation, SpeculativeBindingOption.BindAsExpression);
        }

        var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

        if (methodSymbol == null)
        {
            Console.WriteLine("No method symbol found.");
            return;
        }

        // Step 6: Retrieve method overloads using LookupSymbols
        var methodGroup = semanticModel.LookupSymbols(position, methodSymbol.ContainingType, methodSymbol.Name)
                                       .OfType<IMethodSymbol>();

        // Step 7: Print method overloads and parameters
        foreach (var overload in methodGroup)
        {
            var parameters = overload.Parameters.Select(p => $"{p.Type} {p.Name}");
            Console.WriteLine($"{overload.Name}({string.Join(", ", parameters)})");
        }
    }

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


    class CompletionItemSingleLetterMatchSorter
    {
        StringComparerWithBiasForSingleLetter stringComparerWithBiasForSingleLetter;

        public CompletionItemSingleLetterMatchSorter(char singleLetter)
        {
            stringComparerWithBiasForSingleLetter = new StringComparerWithBiasForSingleLetter(singleLetter);
        }

        public int Compare(CompletionItem left, CompletionItem right)
        {
            return stringComparerWithBiasForSingleLetter.Compare(left.DisplayText, right.DisplayText);
        }
    }


    public class StringComparerWithBiasForSingleLetter
    {
        private char singleLetterInLowerCaseAsChar;
        private char singleLetterInUpperCaseAsChar;
        private string singleLetterInLowerCaseAsString;
        private string singleLetterInUpperCaseAsString;

        public StringComparerWithBiasForSingleLetter(char singleLetter)
        {
            singleLetterInLowerCaseAsChar = char.ToLower(singleLetter);
            singleLetterInUpperCaseAsChar = char.ToUpper(singleLetter);
            singleLetterInLowerCaseAsString = singleLetterInLowerCaseAsChar.ToString();
            singleLetterInUpperCaseAsString = singleLetterInUpperCaseAsChar.ToString();
        }

        public int Compare(string left, string right)
        {
            // LB   RB  LC  RC  R
            // y    y   na  na  cmp(l, r)
            // y    n   na  na  l
            // n    y   na  na  r
            // n    n   y   n   l
            // n    n   n   y   r
            // n    n   y   y   cmp(l, r)
            // n    n   n   n   cmp(l, r)

            var leftContainsLetter = left.Contains(singleLetterInUpperCaseAsChar) || left.Contains(singleLetterInLowerCaseAsChar);
            var rightContainsLetter = right.Contains(singleLetterInUpperCaseAsChar) || right.Contains(singleLetterInLowerCaseAsChar);
            var leftBeginsWithLetter = left.StartsWith(singleLetterInUpperCaseAsString) || left.StartsWith(singleLetterInLowerCaseAsString);
            var rightBeginsWithLetter = right.StartsWith(singleLetterInUpperCaseAsString) || right.StartsWith(singleLetterInLowerCaseAsString);

            if (leftBeginsWithLetter)
            {
                if (rightBeginsWithLetter)
                {
                    return left.CompareTo(right);
                }
                else
                {
                    return -1;
                }
            }

            if (rightBeginsWithLetter)
            {
                return 1;
            }

            if (leftContainsLetter && !rightContainsLetter)
            {
                return -1;
            }

            if (!leftContainsLetter && rightContainsLetter)
            {
                return 1;
            }

            return left.CompareTo(right);
        }
    }






    public async Task Test(int position)
    {
        SyntaxTree syntaxTree = await GetSyntaxTreeAsync();

        var compilation = CSharpCompilation.Create("ScriptAnalysis")
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        SemanticModel semanticModel = await GetSemanticModelAsync();

        SyntaxNode root = syntaxTree.GetRoot();
        SyntaxToken token = root.FindToken(position);
        SyntaxNode node = token.Parent;

        while (node != null && !(node is InvocationExpressionSyntax))
        {
            node = node.Parent;
        }
    }

    public async Task GetSuggestionsAsync(int position)
    {
        var xmlInfo = XMLHelpManager.Test(
            syntaxTree: await GetSyntaxTreeAsync(),
            semanticModel: await GetSemanticModelAsync(),
            position: position);

        var comp = await this.GetCompletionSuggestionsAsync(position: position);
    }
}
