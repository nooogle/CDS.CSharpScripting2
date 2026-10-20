using CDS.CSharpScript2.Documentation;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// Serves documentation pages from an editor's current manager, first bringing the manager's
/// document level with the text on screen.
/// </summary>
internal sealed class EditorDocPageSource : IDocPageSource
{
    private readonly AvaloniaScriptEditor _editor;

    public EditorDocPageSource(AvaloniaScriptEditor editor)
    {
        _editor = editor;
    }

    public async Task<DocPage?> GetDocPageAsync(int position, CancellationToken cancellationToken)
    {
        await _editor.EnsureManagerDocumentCurrentAsync(cancellationToken);

        // Read per call: setting the editor's Environment replaces the manager.
        var manager = _editor.Manager;
        return manager is null ? null : await manager.GetDocPageAsync(position, cancellationToken);
    }

    public async Task<DocPage?> GetDocPageAsync(DocTarget target, CancellationToken cancellationToken)
    {
        await _editor.EnsureManagerDocumentCurrentAsync(cancellationToken);

        var manager = _editor.Manager;
        return manager is null ? null : await manager.GetDocPageAsync(target, cancellationToken);
    }
}
