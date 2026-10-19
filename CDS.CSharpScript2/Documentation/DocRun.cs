namespace CDS.CSharpScript2.Documentation;

/// <summary>A span of documentation text with a presentation style.</summary>
/// <param name="Text">The text to show.</param>
/// <param name="Kind">How the text is presented.</param>
/// <param name="Target">The page a <see cref="DocRunKind.Link"/> run navigates to; otherwise <see langword="null"/>.</param>
public sealed record DocRun(string Text, DocRunKind Kind, DocTarget? Target);
