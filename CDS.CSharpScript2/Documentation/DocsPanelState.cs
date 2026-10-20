namespace CDS.CSharpScript2.Documentation;

/// <summary>The mode a documentation panel is in.</summary>
public enum DocsPanelState
{
    /// <summary>The panel is hidden.</summary>
    Closed,

    /// <summary>The panel is open and tracks the symbol at the caret.</summary>
    Following,

    /// <summary>The user followed a link, so the panel stays on the pages they chose.</summary>
    Browsing,
}
