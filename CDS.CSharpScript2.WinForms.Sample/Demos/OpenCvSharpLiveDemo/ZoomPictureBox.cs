using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace CDS.CSharpScript2.WinForms.Sample.Demos.OpenCvSharpLiveDemo;

/// <summary>
/// A panel that renders an image with interactive pan and zoom.
/// Mouse-wheel zooms centred on the cursor; left-drag pans; double-click fits the image.
/// When <see cref="IsInteractive"/> is <see langword="false"/> the control renders the current
/// viewport state but ignores all mouse input — use <see cref="SetViewport"/> to drive it.
/// </summary>
internal sealed class ZoomPictureBox : Control
{
    private static readonly Color s_background = Color.FromArgb(30, 30, 30);
    private const float MinZoom = 0.02f;
    private const float MaxZoom = 32f;

    private Image? _image;
    private float _zoom = 1f;
    private PointF _pan;
    private Point _lastMousePosition;
    private bool _isDragging;

    /// <summary>
    /// Gets or sets whether this control accepts mouse input for pan and zoom.
    /// Set to <see langword="false"/> on the slave control so only the master drives the viewport.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsInteractive { get; set; } = true;

    /// <summary>
    /// Gets or sets the image to display. The control takes ownership: the previous image is
    /// disposed automatically when a new one is assigned or when the control is disposed.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image? Image
    {
        get => _image;
        set
        {
            var prev = _image;
            _image = value;
            prev?.Dispose();
            Invalidate();
        }
    }

    /// <summary>Gets the current zoom factor (1.0 = 100 %).</summary>
    public float Zoom => _zoom;

    /// <summary>Gets the current pan offset in screen pixels.</summary>
    public PointF Pan => _pan;

    /// <summary>Raised when the viewport changes due to user interaction.</summary>
    public event EventHandler? ViewportChanged;

    /// <summary>
    /// Initializes a new instance of <see cref="ZoomPictureBox"/>.
    /// </summary>
    public ZoomPictureBox()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            value: true);
        UpdateStyles();
    }

    /// <summary>
    /// Scales and centres the image to fit within the control bounds, then raises
    /// <see cref="ViewportChanged"/> so any linked slave controls can follow.
    /// </summary>
    public void FitToControl()
    {
        if (_image == null || Width <= 0 || Height <= 0) { return; }

        _zoom = Math.Min((float)Width / _image.Width, (float)Height / _image.Height);
        _pan = new PointF(
            (Width - _image.Width * _zoom) / 2f,
            (Height - _image.Height * _zoom) / 2f);

        Invalidate();
        ViewportChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Applies a viewport from an external source without raising <see cref="ViewportChanged"/>.
    /// Used by slave controls that mirror the master.
    /// </summary>
    /// <param name="zoom">The zoom factor to apply.</param>
    /// <param name="pan">The pan offset to apply.</param>
    public void SetViewport(float zoom, PointF pan)
    {
        _zoom = zoom;
        _pan = pan;
        Invalidate();
    }

    // ── Painting ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(s_background);

        if (_image == null) { return; }

        g.InterpolationMode = InterpolationMode.Bilinear;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        g.DrawImage(
            _image,
            new RectangleF(_pan.X, _pan.Y, _image.Width * _zoom, _image.Height * _zoom));
    }

    // ── Mouse interaction ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (IsInteractive) { Focus(); }
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!IsInteractive) { return; }

        float factor = e.Delta > 0 ? 1.1f : 1f / 1.1f;
        float newZoom = Math.Min(MaxZoom, Math.Max(MinZoom, _zoom * factor));

        // Keep the pixel under the cursor fixed in screen space.
        var imagePoint = new PointF((e.X - _pan.X) / _zoom, (e.Y - _pan.Y) / _zoom);
        _pan = new PointF(e.X - imagePoint.X * newZoom, e.Y - imagePoint.Y * newZoom);
        _zoom = newZoom;

        Invalidate();
        ViewportChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!IsInteractive || e.Button != MouseButtons.Left) { return; }

        _isDragging = true;
        _lastMousePosition = e.Location;
        Cursor = Cursors.Hand;
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isDragging) { return; }

        _pan = new PointF(
            _pan.X + e.X - _lastMousePosition.X,
            _pan.Y + e.Y - _lastMousePosition.Y);
        _lastMousePosition = e.Location;

        Invalidate();
        ViewportChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_isDragging) { return; }

        _isDragging = false;
        Cursor = Cursors.Default;
    }

    /// <inheritdoc/>
    protected override void OnDoubleClick(EventArgs e)
    {
        base.OnDoubleClick(e);
        if (IsInteractive) { FitToControl(); }
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image?.Dispose();
            _image = null;
        }

        base.Dispose(disposing);
    }
}
