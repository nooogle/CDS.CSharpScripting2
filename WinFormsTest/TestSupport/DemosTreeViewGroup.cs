namespace WinFormsTest.TestSupport;

/// <summary>
/// Represents a group of demos in the DemosTreeView.
/// </summary>
public class DemosTreeViewGroup
{
    private TreeNode treeNode;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemosTreeViewGroup"/> class.
    /// </summary>
    /// <param name="treeNode">The tree node representing this group.</param>
    public DemosTreeViewGroup(TreeNode treeNode)
    {
        this.treeNode = treeNode;
    }

    /// <summary>
    /// Adds a new group to this group.
    /// </summary>
    /// <param name="name">The name of the new group.</param>
    /// <returns>A <see cref="DemosTreeViewGroup"/> representing the added group.</returns>
    public DemosTreeViewGroup AddGroup(string name)
    {
        TreeNode groupTreeNode = treeNode.Nodes.Add(name);
        return new DemosTreeViewGroup(groupTreeNode);
    }

    /// <summary>
    /// Adds a new demo to this group.
    /// </summary>
    /// <param name="name">The name of the demo.</param>
    /// <param name="tooltip">The tooltip text for the demo.</param>
    /// <param name="parent">The parent form for the demo.</param>
    /// <param name="createForm">A function to create the form for the demo.</param>
    public void AddDemo(string name, string tooltip, Form parent, Func<Form> createForm)
    {
        _ = new DemosTreeViewDemo(name, tooltip, treeNode, () => RunModalForm(parent, createForm()));
    }

    /// <summary>
    /// Runs the specified form as a modal dialog.
    /// </summary>
    /// <param name="parent">The parent form.</param>
    /// <param name="form">The form to run.</param>
    private void RunModalForm(Form parent, Form form)
    {
        form.ShowDialog(parent);
        form.Dispose();
    }
}




