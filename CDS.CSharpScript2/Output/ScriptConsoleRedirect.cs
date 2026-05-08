using System.Text;

namespace CDS.CSharpScript2.Output;

/// <summary>
/// Redirects <see cref="Console.Out"/> to a caller-supplied handler for the
/// lifetime of the instance. Dispose to restore the original output.
/// </summary>
/// <remarks>
/// Typical usage — capture script console output to an output panel:
/// <code>
/// using var redirect = new ScriptConsoleRedirect(outputPanel.Append);
/// await compiled.RunAsync(globals);
/// </code>
/// </remarks>
public class ScriptConsoleRedirect : TextWriter
{
    private readonly Action<string?> _writeString;
    private readonly TextWriter _originalOut;

    /// <summary>
    /// Initialises a new redirect and immediately installs it as <see cref="Console.Out"/>.
    /// </summary>
    /// <param name="writeString">Handler invoked for each string written to the console.</param>
    public ScriptConsoleRedirect(Action<string?> writeString)
    {
        _writeString = writeString ?? throw new ArgumentNullException(nameof(writeString));
        _originalOut = Console.Out;
        Console.SetOut(this);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Console.SetOut(_originalOut);
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override void Write(char value) => _writeString(value.ToString());

    /// <inheritdoc/>
    public override void Write(string? value) => _writeString(value);

    /// <inheritdoc/>
    public override void Write(char[]? buffer, int index, int count)
    {
        if (buffer != null)
            _writeString(new string(buffer, index, count));
    }

    /// <inheritdoc/>
    public override Encoding Encoding => Encoding.Unicode;
}
