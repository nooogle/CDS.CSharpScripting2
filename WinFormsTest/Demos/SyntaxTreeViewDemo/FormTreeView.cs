using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.ComponentModel;

namespace WinFormsTest.Demos.SyntaxTreeViewDemo;

public partial class FormTreeView : Form
{
    private CDS.CSharpScript2.Editors.EditorManager editorManager;
    private readonly Settings settings;
    private CDS.CSharpScript2.CompiledScript? compiledScript;

    public FormTreeView(Settings settings)
    {
        InitializeComponent();
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        InitialiseScript();
    }

    private void InitialiseScript()
    {
        editorManager = new CDS.CSharpScript2.Editors.EditorManager(
            environment: CDS.CSharpScript2.ScriptEnvironment.Default,
            scintillaScriptEditor.ApplyDiagnostics,
            scintillaScriptEditor.ApplyClassifications);

        scintillaScriptEditor.SetDelegates(
            editorManager.ApplyScript,
            editorManager.GetAutoCompletions,
            editorManager.GetAPIInfo);

        scintillaScriptEditor.Script = settings.Script;

        //RefreshTree().GetAwaiter().GetResult();
    }



    /// <summary>
    /// Saves the current script to settings when the form is closing.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        settings.Script = scintillaScriptEditor.Script;
    }

    private async void scintillaScriptEditor_OnScriptChanged(object sender, EventArgs e)
    {
        await RefreshTree();
    }

    private async Task RefreshTree()
    {
        while (!editorManager.IsReady)
        {
            await Task.Delay(250);
        }

        compiledScript = await editorManager.GetCompiledScriptAsync();
        
        var root = compiledScript.SyntaxTree.GetRoot();

        treeView1.Nodes.Clear();
        var rootNode = new TreeNode(GetSyntaxNodeInfo(root))
        {
            Tag = root
        };
        treeView1.Nodes.Add(rootNode);
        PopulateTreeView(root, rootNode);
        treeView1.ExpandAll();
    }

    private void PopulateTreeView(Microsoft.CodeAnalysis.SyntaxNode node, TreeNode treeNode)
    {
        foreach (var child in node.ChildNodesAndTokens())
        {


            var childNode = new TreeNode(GetSyntaxNodeInfo(child))
            {
                Tag = child
            };
            treeNode.Nodes.Add(childNode);
            if (child.IsNode)
            {
                PopulateTreeView(child.AsNode(), childNode);
            }
        }
    }


    private string GetSyntaxNodeInfo(Microsoft.CodeAnalysis.SyntaxNodeOrToken node)
    {
        if(node.IsToken) { return $"{node.Kind()} (token)"; }
        var syntaxNode = (SyntaxNode)node;

        var symbolInfo = compiledScript.SemanticModel.GetSymbolInfo(syntaxNode);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

        // Fallback: if it's a 'var' or similar, GetTypeInfo helps
        var typeInfo = compiledScript.SemanticModel.GetTypeInfo(syntaxNode);

        // Decide a classification
        string classification = Classify(symbol, typeInfo);

        return $"{node.Kind()} ({classification})";
    }

    static string Classify(ISymbol? symbol, TypeInfo typeInfo)
    {
        if (symbol == null)
        {
            // Unresolved or contextual keywords; you might want to look at typeInfo here
            return "Unresolved";
        }

        switch (symbol)
        {
            case IMethodSymbol _:
                return "Method";
            case IPropertySymbol _:
                return "Property";
            case IFieldSymbol _:
                return "Field";
            case IEventSymbol _:
                return "Event";
            case INamespaceSymbol _:
                return "Namespace";
            case ITypeSymbol t:
                // Distinguish class/struct/interface/enum/delegate
                return t.TypeKind switch
                {
                    TypeKind.Class => "Class",
                    TypeKind.Struct => "Struct",
                    TypeKind.Interface => "Interface",
                    TypeKind.Enum => "Enum",
                    TypeKind.Delegate => "Delegate",
                    TypeKind.TypeParameter => "Type Parameter",
                    _ => "Type"
                };
            case IParameterSymbol _:
                return "Parameter";
            case ILocalSymbol _:
                return "Local";
            default:
                return symbol.Kind.ToString();
        }
    }

    private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
    {
        var treeNode = e.Node?.Tag;
        var didHighlight = false;

        if (treeNode != null)
        {
            if (treeNode is Microsoft.CodeAnalysis.SyntaxNodeOrToken)
            {
                var syntaxNodeOrToken = (Microsoft.CodeAnalysis.SyntaxNodeOrToken)treeNode;
                scintillaScriptEditor.HighlightText(syntaxNodeOrToken.SpanStart, syntaxNodeOrToken.Span.Length);
                didHighlight = true;
            }
        }

        if(!didHighlight)
        {
            scintillaScriptEditor.ClearHighlightText();
        }
    }
}
