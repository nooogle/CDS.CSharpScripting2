using CDS.CSharpScript2.APIInfo;

namespace CDS.CSharpScript2.ScintillaEditor;

public partial class FormAPIInfo : Form
{
    public FormAPIInfo()
    {
        InitializeComponent();
    }


    public void ShowAPIInfo(IWin32Window parent, APIInfo.IAPIInfoResult apiInfo, Point location)
    {
        bool didSetAPIInfo = false;

        if (apiInfo?.TypeInfo != null)
        {
            bool isTypeOnly = !apiInfo.MemberInfos.Any();

            if (isTypeOnly)
            {
                SetTypeOnly(apiInfo.TypeInfo);
                didSetAPIInfo = true;
            }
            else
            {
                SetMemberInfo(apiInfo);
                didSetAPIInfo = true;
            }
        }

        if (didSetAPIInfo)
        {
            Show(parent);
        }
        else
        {
            Hide();
        }
    }

    private void SetMemberInfo(IAPIInfoResult apiInfo)
    {
        richTextBox.Clear();
        richTextBox.AppendText($"{apiInfo.TypeInfo.Name} ({apiInfo.TypeInfo.TypeKind})\n\n");

        foreach (var memberInfo in apiInfo.MemberInfos)
        {
            //richTextBox.AppendText($"{memberInfo.} {memberInfo.Name}\n");
            richTextBox.AppendText($"{memberInfo.Signature}\n");
            if (!string.IsNullOrWhiteSpace(memberInfo.Remarks))
            {
                richTextBox.AppendText($"{memberInfo.Remarks}\n");
            }
            //richTextBox.AppendText("\n");
        }

        // add the above to the treeview
        treeView.Nodes.Clear();

        var typeNode = new TreeNode(apiInfo.TypeInfo.Name);
        typeNode.Tag = apiInfo.TypeInfo;
        typeNode.Nodes.Add(new TreeNode($"Namespace: {apiInfo.TypeInfo.Namespace}"));
        typeNode.Nodes.Add(new TreeNode($"Type: {apiInfo.TypeInfo.TypeKind}"));
        typeNode.Nodes.Add(new TreeNode($"Name: {apiInfo.TypeInfo.Name}"));
        typeNode.Nodes.Add(new TreeNode($"Summary: {apiInfo.TypeInfo.Summary}"));
        if (!string.IsNullOrWhiteSpace(apiInfo.TypeInfo.Remarks))
        {
            typeNode.Nodes.Add(new TreeNode($"Remarks: {apiInfo.TypeInfo.Remarks}"));
        }

        treeView.Nodes.Add(typeNode);

        var membersNode = new TreeNode("Members");

        foreach (var memberInfo in apiInfo.MemberInfos)
        {
            var memberNode = new TreeNode(memberInfo.Signature);
            memberNode.Tag = memberInfo;

            // add summary
            if (!string.IsNullOrWhiteSpace(memberInfo.Summary))
            {
                memberNode.Nodes.Add(new TreeNode($"Summary: {memberInfo.Summary}"));
            }

            if (!string.IsNullOrWhiteSpace(memberInfo.Remarks))
            {
                memberNode.Nodes.Add(new TreeNode($"Remarks: {memberInfo.Remarks}"));
            }

            // add parameters
            if (memberInfo.Parameters != null && memberInfo.Parameters.Count > 0)
            {
                var parametersNode = new TreeNode("Parameters");
                foreach (var parameter in memberInfo.Parameters)
                {
                    var parameterNode = new TreeNode($"{parameter.Name} ({parameter.Type})");
                    parameterNode.Tag = parameter;
                    parametersNode.Nodes.Add(parameterNode);

                    if(!string.IsNullOrWhiteSpace(parameter.Documentation))
                    {
                        parameterNode.Nodes.Add(new TreeNode(parameter.Documentation));
                    }

                }
                memberNode.Nodes.Add(parametersNode);
            }

            membersNode.Nodes.Add(memberNode);
        }

        treeView.Nodes.Add(membersNode);

        membersNode.Expand();

    }

    private void SetTypeOnly(DetailedTypeInfo typeInfo)
    {
        richTextBox.Clear();
        richTextBox.AppendText($"Namespace {typeInfo.Namespace}\n");
        richTextBox.AppendText($"Type      {typeInfo.TypeKind}\n");
        richTextBox.AppendText($"Name      {typeInfo.Name}\n");

        richTextBox.AppendText($"\n{typeInfo.Summary}\n");

        if (!string.IsNullOrWhiteSpace(typeInfo.Remarks))
        {
            richTextBox.AppendText($"\n{typeInfo.Remarks}\n");
        }

        // add the same info to the treeView
        treeView.Nodes.Clear();
        var node = new TreeNode(typeInfo.Name);
        node.Tag = typeInfo;
        node.Nodes.Add(new TreeNode($"Namespace: {typeInfo.Namespace}"));
        node.Nodes.Add(new TreeNode($"Type: {typeInfo.TypeKind}"));
        node.Nodes.Add(new TreeNode($"Name: {typeInfo.Name}"));
        node.Nodes.Add(new TreeNode($"Summary: {typeInfo.Summary}"));
        if (!string.IsNullOrWhiteSpace(typeInfo.Remarks))
        {
            node.Nodes.Add(new TreeNode($"Remarks: {typeInfo.Remarks}"));
        }
    }

    private void FormAPIInfo_KeyDown(object sender, KeyEventArgs e)
    {
        if(e.KeyCode == Keys.Escape)
        {
            Hide();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
    }
}
