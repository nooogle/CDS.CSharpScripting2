namespace CDS.CSharpScript2.ScintillaEditor.CustomToolTip;

public partial class SignatureHelpView : UserControl
{

    private int _index;
    private IReadOnlyList<SignatureItem> _items = Array.Empty<SignatureItem>();

    public SignatureHelpView()
    {
        InitializeComponent();
    }

    private void btnPrevious_Click(object sender, EventArgs e)
    {
        Move(-1);
    }

    private void btnNext_Click(object sender, EventArgs e)
    {
        Move(1);
    }

    public void SetItems(IReadOnlyList<SignatureItem> items, int currentIndex)
    {
        _items = items;
        _index = Math.Max(0, Math.Min(currentIndex, _items.Count - 1));
        Render();
    }

    public void Move(int delta)
    {
        if (_items.Count == 0) return;
        _index = (_index + delta + _items.Count) % _items.Count;
        Render();
    }

    public void HighlightParameter(int paramIndex)
    {
        if (_items.Count == 0) return;
        Render(paramIndex);
    }

    private void Render(int? activeParam = null)
    {
        var it = _items[_index];
        labelHeader.Text = $"Overload {_index + 1} of {_items.Count}";
        rtfInfo.SuspendLayout();
        rtfInfo.Clear();

        // Example: bold the active parameter
        rtfInfo.SelectionFont = Font;
        rtfInfo.AppendText(it.Prefix); // e.g., "void Foo("

        for (int i = 0; i < it.Parameters.Count; i++)
        {
            var p = it.Parameters[i];
            if (i == activeParam) rtfInfo.SelectionFont = new Font(Font, FontStyle.Bold);
            rtfInfo.AppendText(p.Display);
            rtfInfo.SelectionFont = Font;
            if (i < it.Parameters.Count - 1) rtfInfo.AppendText(", ");
        }

        rtfInfo.AppendText(it.Suffix); // e.g., ")"
        if (!string.IsNullOrWhiteSpace(it.Documentation))
        {
            rtfInfo.AppendText(Environment.NewLine + Environment.NewLine + it.Documentation);
        }
        rtfInfo.ResumeLayout();
    }
}
