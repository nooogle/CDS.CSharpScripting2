using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CDS.CSharpScript2.Documentation;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// A clickable reference to another documentation page: text in the link colour with a hand
/// cursor, underlined under the pointer. A left click raises the bubbling <see cref="ClickEvent"/>,
/// which the panel handles once for all its links.
/// </summary>
internal sealed class DocLink : TextBlock
{
    /// <summary>Raised when the link is clicked with the left button.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<DocLink, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private bool _pressed;

    public DocLink(string text, DocTarget target)
    {
        Text = text;
        Target = target;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    /// <summary>Raised when the link is clicked with the left button.</summary>
    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>The page the link leads to.</summary>
    public DocTarget Target { get; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);

        // Inside a paragraph (an InlineUIContainer) the link sits on the line's baseline by its
        // bottom edge unless it reports its own baseline; report it so its text lines up.
        if (TextLayout.TextLines.Count > 0)
        {
            BaselineOffset = TextLayout.TextLines[0].Baseline + Padding.Top;
        }

        return size;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        TextDecorations = Avalonia.Media.TextDecorations.Underline;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        TextDecorations = null;
        _pressed = false;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _pressed = true;
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pressed && e.InitialPressMouseButton == MouseButton.Left)
        {
            _pressed = false;
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        }
    }
}
