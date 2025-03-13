using WinFormsTest.TestSupport;

namespace WinFormsTest;

/// <summary>
/// Represents a tree view control for displaying demo groups and demos.
/// Useful for apps that want to host a collection of demos and tests.
/// </summary>
public partial class DemosTreeView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DemosTreeView"/> class.
    /// </summary>
    public DemosTreeView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Adds a new group to the tree view.
    /// </summary>
    /// <param name="name">The name of the group.</param>
    /// <returns>A <see cref="DemosTreeViewGroup"/> representing the added group.</returns>
    public DemosTreeViewGroup AddGroup(string name)
    {
        TreeNode groupTreeNode = treeView.Nodes.Add(name);
        return new DemosTreeViewGroup(groupTreeNode);
    }

    /// <summary>
    /// Handles the NodeMouseDoubleClick event of the tree view.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="TreeNodeMouseClickEventArgs"/> instance containing the event data.</param>
    private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        var action = e.Node.Tag as Action;
        action?.Invoke();
    }

    /// <summary>
    /// Handles the KeyPress event of the tree view.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="KeyPressEventArgs"/> instance containing the event data.</param>
    private void treeView_KeyPress(object sender, KeyPressEventArgs e)
    {
        var isEnterKey = (e.KeyChar == '\r');
        if (!isEnterKey) { return; }

        var action = treeView.SelectedNode?.Tag as Action;
        action?.Invoke();
    }

    /// <summary>
    /// Expands all groups in the tree view.
    /// </summary>
    public void ExpandAllGroups()
    {
        treeView.ExpandAll();
    }
}



