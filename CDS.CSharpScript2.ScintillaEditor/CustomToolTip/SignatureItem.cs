namespace CDS.CSharpScript2.ScintillaEditor.CustomToolTip;

public sealed class SignatureItem(
    string prefix, 
    IReadOnlyList<Param> parameters, 
    string suffix, 
    string documentation)
{
    public string Prefix { get; } = prefix;
    public IReadOnlyList<Param> Parameters { get; } = parameters;
    public string Suffix { get; } = suffix;
    public string Documentation { get; } = documentation;
}
