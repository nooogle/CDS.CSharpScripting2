using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.ComponentModel;

namespace WinFormsTest.Demos.SyntaxTreeViewDemo;

public partial class FormTreeView : Form
{
    private CDS.CSharpScript2.Editors.EditorManager editorManager;
    private readonly Settings settings;
    private SemanticModel? _semanticModel;

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
    }

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

        var syntaxTree = await editorManager.GetSyntaxTreeAsync();
        _semanticModel = await editorManager.GetSemanticModelAsync();

        if (syntaxTree == null) return;

        var root = syntaxTree.GetRoot();

        treeView1.Nodes.Clear();
        var rootNode = new TreeNode(GetSyntaxNodeInfo(root)) { Tag = root };
        treeView1.Nodes.Add(rootNode);
        PopulateTreeView(root, rootNode);
        treeView1.ExpandAll();
    }

    private void PopulateTreeView(SyntaxNode node, TreeNode treeNode)
    {
        foreach (var child in node.ChildNodesAndTokens())
        {
            var childNode = new TreeNode(GetSyntaxNodeInfo(child)) { Tag = child };
            treeNode.Nodes.Add(childNode);
            if (child.IsNode)
                PopulateTreeView(child.AsNode(), childNode);
        }
    }

    private string GetSyntaxNodeInfo(SyntaxNodeOrToken node)
    {
        if (node.IsToken) return $"{node.Kind()} (token)";

        var syntaxNode = (SyntaxNode)node;
        if (_semanticModel == null) return $"{node.Kind()} (Unresolved)";

        var symbolInfo = _semanticModel.GetSymbolInfo(syntaxNode);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        var typeInfo = _semanticModel.GetTypeInfo(syntaxNode);

        return $"{node.Kind()} ({Classify(symbol, typeInfo)})";
    }

    static string Classify(ISymbol? symbol, TypeInfo typeInfo)
    {
        if (symbol == null) return "Unresolved";

        return symbol switch
        {
            IMethodSymbol => "Method",
            IPropertySymbol => "Property",
            IFieldSymbol => "Field",
            IEventSymbol => "Event",
            INamespaceSymbol => "Namespace",
            ITypeSymbol t => t.TypeKind switch
            {
                TypeKind.Class => "Class",
                TypeKind.Struct => "Struct",
                TypeKind.Interface => "Interface",
                TypeKind.Enum => "Enum",
                TypeKind.Delegate => "Delegate",
                TypeKind.TypeParameter => "Type Parameter",
                _ => "Type"
            },
            IParameterSymbol => "Parameter",
            ILocalSymbol => "Local",
            _ => symbol.Kind.ToString()
        };
    }

    private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
    {
        var treeNode = e.Node?.Tag;
        var didHighlight = false;

        if (treeNode is SyntaxNodeOrToken syntaxNodeOrToken)
        {
            scintillaScriptEditor.HighlightText(syntaxNodeOrToken.SpanStart, syntaxNodeOrToken.Span.Length);
            didHighlight = true;
        }

        if (!didHighlight)
            scintillaScriptEditor.ClearHighlightText();
    }
}
