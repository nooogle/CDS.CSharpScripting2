using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.ComponentModel;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.SyntaxTreeViewDemo;

public partial class FormTreeView : Form
{
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

        scintillaScriptEditor.API.Environment = CDS.CSharpScript2.ScriptEnvironment.Default;
        scintillaScriptEditor.API.Script = settings.Script;
        scintillaScriptEditor.ScriptChanged += async (_, _) => await RefreshTree();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        settings.Script = scintillaScriptEditor.API.Script;
    }

    // ScriptChanged fires after analysis completes, so Manager is ready immediately.
    private async Task RefreshTree()
    {
        var manager = scintillaScriptEditor.API.Manager;
        if (manager is null) return;

        var syntaxTree = await manager.GetSyntaxTreeAsync();
        _semanticModel = await manager.GetSemanticModelAsync();

        if (syntaxTree is null) return;

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
                PopulateTreeView(child.AsNode()!, childNode);
        }
    }

    private string GetSyntaxNodeInfo(SyntaxNodeOrToken node)
    {
        if (node.IsToken) return $"{node.Kind()} (token)";

        var syntaxNode = node.AsNode();
        if (syntaxNode is null || _semanticModel is null) return $"{node.Kind()} (Unresolved)";

        var symbolInfo = _semanticModel.GetSymbolInfo(syntaxNode);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        var typeInfo = _semanticModel.GetTypeInfo(syntaxNode);

        return $"{node.Kind()} ({Classify(symbol, typeInfo)})";
    }

    private static string Classify(ISymbol? symbol, TypeInfo typeInfo)
    {
        if (symbol is null) return "Unresolved";

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
            scintillaScriptEditor.API.HighlightText(syntaxNodeOrToken.SpanStart, syntaxNodeOrToken.Span.Length);
            didHighlight = true;
        }

        if (!didHighlight)
            scintillaScriptEditor.API.ClearHighlightText();
    }
}
