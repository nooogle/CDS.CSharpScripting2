using System.ComponentModel;
using System.Runtime.InteropServices;
using CDS.CSharpScript2.Documentation;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// The docs panel's read-only text body: it knows where its links are, shows a hand over them,
/// raises <see cref="DocLinkClicked"/> for a click, and hides the (useless) text caret.
/// </summary>
internal sealed class DocTextBox : RichTextBox
{
    private const int WmSetFocus = 0x0007;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int EmGetScrollPos = 0x0400 + 221;
    private const int EmSetScrollPos = 0x0400 + 222;

    private IReadOnlyList<DocLinkSpan> _links = [];

    public DocTextBox()
    {
        ReadOnly = true;
        BorderStyle = BorderStyle.None;
        DetectUrls = false;
        ScrollBars = RichTextBoxScrollBars.Vertical;
        WordWrap = true;
        Cursor = Cursors.IBeam;
    }

    /// <summary>Raised when a link is clicked with the left button (and no text was being selected).</summary>
    public event EventHandler<DocTarget>? DocLinkClicked;

    /// <summary>Gets or sets the links in the current text.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal IReadOnlyList<DocLinkSpan> Links
    {
        get => _links;
        set => _links = value;
    }

    /// <summary>Gets or sets how far the text is scrolled, in pixels from the top.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ScrollOffset
    {
        get
        {
            var point = default(Point);
            if (IsHandleCreated)
            {
                SendMessage(Handle, EmGetScrollPos, IntPtr.Zero, ref point);
            }

            return point.Y;
        }

        set
        {
            if (IsHandleCreated)
            {
                var point = new Point(0, value);
                SendMessage(Handle, EmSetScrollPos, IntPtr.Zero, ref point);
            }
        }
    }

    /// <summary>Finds the link under a point in the control's client area.</summary>
    /// <param name="location">The point to test.</param>
    /// <param name="target">The link's target, when there is one.</param>
    /// <returns><see langword="true"/> when the point is over a link.</returns>
    public bool TryGetLinkAt(Point location, out DocTarget target)
    {
        if (_links.Count > 0 && TextLength > 0)
        {
            var index = GetCharIndexFromPosition(location);
            foreach (var link in _links)
            {
                if (link.Contains(index) && IsOverCharacter(index, location))
                {
                    target = link.Target;
                    return true;
                }
            }
        }

        target = null!;
        return false;
    }

    /// <summary>
    /// Whether <paramref name="location"/> is actually over the character at <paramref name="index"/>.
    /// <see cref="RichTextBox.GetCharIndexFromPosition"/> returns the nearest character, so empty space
    /// beside the end of a line or below the text would otherwise count as the last character there.
    /// </summary>
    private bool IsOverCharacter(int index, Point location)
    {
        var start = GetPositionFromCharIndex(index);
        var lineHeight = Font.Height + 4;
        if (location.Y < start.Y - 2 || location.Y >= start.Y + lineHeight || location.X < start.X)
        {
            return false;
        }

        // Within a line the next character's left edge is this one's right edge. At the end of a line
        // there is no such edge, so allow about one character's width.
        var next = GetPositionFromCharIndex(index + 1);
        var right = next.Y == start.Y && next.X > start.X ? next.X : start.X + Font.Height;
        return location.X < right;
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var cursor = TryGetLinkAt(e.Location, out _) ? Cursors.Hand : Cursors.IBeam;
        if (!ReferenceEquals(Cursor, cursor))
        {
            Cursor = cursor;
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left && SelectionLength == 0 && TryGetLinkAt(e.Location, out var target))
        {
            DocLinkClicked?.Invoke(this, target);
        }
    }

    /// <inheritdoc/>
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WmSetFocus || m.Msg == WmKeyDown || m.Msg == WmKeyUp || m.Msg == WmLButtonDown || m.Msg == WmLButtonUp)
        {
            HideCaret(Handle);
        }
    }

    /// <inheritdoc/>
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);

        // Loading a new page can bring the caret back while the box has focus.
        if (Focused)
        {
            HideCaret(Handle);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref Point lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool HideCaret(IntPtr hWnd);
}
