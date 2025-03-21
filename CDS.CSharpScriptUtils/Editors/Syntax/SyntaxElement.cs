using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDS.CSharpScriptUtils.Editors.Syntax;

public readonly struct SyntaxElement
{
    public SyntaxElement(TextSpan span, SyntaxKind kind)
    {
        Span = span;
        Kind = kind;
    }


    public TextSpan Span { get; }
    public SyntaxKind Kind { get; }

    public override string ToString()
    {
        return $"{Kind} {Span}";
    }

    public static bool operator ==(SyntaxElement a, SyntaxElement b)
    {
        return (a.Kind == b.Kind) && (a.Span == b.Span);
    }

    public static bool operator !=(SyntaxElement a, SyntaxElement b) => !(a == b);


    public override bool Equals(object obj)
    {
        return
            (obj is SyntaxElement) &&
            (((SyntaxElement)obj) == this);
    }

    public override int GetHashCode()
    {
        return Kind.GetHashCode() ^ Span.GetHashCode();
    }
}
