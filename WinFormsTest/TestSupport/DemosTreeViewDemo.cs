namespace WinFormsTest.TestSupport;

/// <summary>
/// Represents a demo item in the DemosTreeView.
/// </summary>
public class DemosTreeViewDemo
{
    private TreeNode treeNode;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemosTreeViewDemo"/> class.
    /// </summary>
    /// <param name="name">The name of the demo.</param>
    /// <param name="tooltip">The tooltip text for the demo.</param>
    /// <param name="parentTreeNode">The parent tree node to which this demo belongs.</param>
    /// <param name="action">The action to execute when the demo is run.</param>
    public DemosTreeViewDemo(string name, string tooltip, TreeNode parentTreeNode, Action action)
    {
        treeNode = parentTreeNode.Nodes.Add(name);
        treeNode.ToolTipText = tooltip;
        treeNode.Tag = action;
    }

    /// <summary>
    /// Runs the demo by invoking the associated action.
    /// </summary>
    public void Run()
    {
        var action = treeNode.Tag as Action;
        action?.Invoke();
    }
}
