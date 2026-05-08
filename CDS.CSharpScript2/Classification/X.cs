//using Microsoft.CodeAnalysis.Classification;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CDS.CSharpScript2.Classification;

//    /// A compact span type for your buffer coordinates.
//    public struct TextSpan(int Start, int Length)
//    {
//        public int End => Start + Length;
//        public override string ToString() => $"[{Start}..{End})";
//    //}

//    /// One base kind per token.
//    public enum TokenKind
//    {
//        // Semantic kinds (preferred)
//        Class, Struct, Interface, Enum, Delegate, TypeParameter, RecordClass, RecordStruct,
//        Method, Property, Field, Event,
//        Namespace,
//        Parameter, Local, Label, Constant,

//        // Lexical kinds (fallbacks)
//        Keyword, Identifier, StringLiteral, NumericLiteral, CharacterLiteral,
//        Operator, Punctuation, Comment, Preprocessor,
//        ExcludedCode, Text,

//        Other
//    }

//    /// Zero or more overlays.
//    [Flags]
//    public enum TokenModifiers
//    {
//        None = 0,
//        Static = 1 << 0,
//        Deprecated = 1 << 1,
//        Extension = 1 << 2
//    }

//    /// Final, editor-facing token descriptor.
//    public struct ClassifiedToken(TextSpan Span, TokenKind Kind, TokenModifiers Modifiers)
//    {
//        public override string ToString() => $"{Span} {Kind} [{Modifiers}]";
//    }

//    /// Maps Roslyn classification strings to your enums and merges overlays.
//    public static class RoslynClassificationMapper
//    {
//        //// Roslyn’s string constants (kept local so your API doesn’t leak Roslyn types).
//        //// Values match Microsoft.CodeAnalysis.Classification.ClassificationTypeNames.
//        //private static class R
//        //{
//        //    // Semantic bases
//        //    public const string ClassName = "class name";
//        //    public const string StructName = "struct name";
//        //    public const string InterfaceName = "interface name";
//        //    public const string EnumName = "enum name";
//        //    public const string DelegateName = "delegate name";
//        //    public const string TypeParameterName = "type parameter name";
//        //    public const string RecordClassName = "record class name";
//        //    public const string RecordStructName = "record struct name";

//        //    public const string MethodName = "method name";
//        //    public const string ExtensionMethodName = "extension method name";
//        //    public const string PropertyName = "property name";
//        //    public const string FieldName = "field name";
//        //    public const string EventName = "event name";

//        //    public const string NamespaceName = "namespace name";
//        //    public const string ParameterName = "parameter name";
//        //    public const string LocalName = "local name";
//        //    public const string LabelName = "label name";
//        //    public const string ConstantName = "constant name";

//        //    // Lexical
//        //    public const string Keyword = "keyword";
//        //    public const string Identifier = "identifier";
//        //    public const string StringLiteral = "string literal";
//        //    public const string NumericLiteral = "numeric literal";
//        //    public const string CharacterLiteral = "character literal";
//        //    public const string Operator = "operator";
//        //    public const string Punctuation = "punctuation";
//        //    public const string Comment = "comment";
//        //    public const string PreprocessorKeyword = "preprocessor keyword";
//        //    public const string ExcludedCode = "excluded code";
//        //    public const string Text = "text";

//        //    // Overlays
//        //    public const string StaticSymbol = "static symbol";
//        //    public const string DeprecatedSymbol = "deprecated symbol";
//        //}

//        // Priority for picking ONE base kind when multiple bases arrive for the same span.
//        private static readonly TokenKind[] BasePriority =
//        {
//            TokenKind.Method, TokenKind.Property, TokenKind.Field, TokenKind.Event,
//            TokenKind.Class, TokenKind.Struct, TokenKind.Interface, TokenKind.Enum, TokenKind.Delegate,
//            TokenKind.RecordClass, TokenKind.RecordStruct, TokenKind.TypeParameter,
//            TokenKind.Namespace, TokenKind.Parameter, TokenKind.Local, TokenKind.Label, TokenKind.Constant,
//            TokenKind.Keyword, TokenKind.Identifier, TokenKind.StringLiteral, TokenKind.NumericLiteral, TokenKind.CharacterLiteral,
//            TokenKind.Operator, TokenKind.Punctuation, TokenKind.Comment, TokenKind.Preprocessor,
//            TokenKind.ExcludedCode, TokenKind.Text,
//            TokenKind.Other
//        };

//        /// Map a single Roslyn classification string into (Kind, Modifiers).
//        /// Use this if you are processing already-merged spans one-by-one.
//        public static (TokenKind Kind, TokenModifiers Modifiers) MapClassification(string classification)
//        {
//            // Overlays first (additive)
//            if (classification == ClassificationTypeNames.StaticSymbol)
//                return (TokenKind.Other, TokenModifiers.Static);
//            if (classification == ClassificationTypeNames.DeprecatedSymbol)
//                return (TokenKind.Other, TokenModifiers.Deprecated);

//            // Extension method is both a base and a modifier (nice to style differently).
//            if (classification == R.ExtensionMethodName)
//                return (TokenKind.Method, TokenModifiers.Extension);

//            // Bases
//            return classification switch
//            {
//                R.ClassName => (TokenKind.Class, TokenModifiers.None),
//                R.StructName => (TokenKind.Struct, TokenModifiers.None),
//                R.InterfaceName => (TokenKind.Interface, TokenModifiers.None),
//                R.EnumName => (TokenKind.Enum, TokenModifiers.None),
//                R.DelegateName => (TokenKind.Delegate, TokenModifiers.None),
//                R.TypeParameterName => (TokenKind.TypeParameter, TokenModifiers.None),
//                R.RecordClassName => (TokenKind.RecordClass, TokenModifiers.None),
//                R.RecordStructName => (TokenKind.RecordStruct, TokenModifiers.None),

//                R.MethodName => (TokenKind.Method, TokenModifiers.None),
//                R.PropertyName => (TokenKind.Property, TokenModifiers.None),
//                R.FieldName => (TokenKind.Field, TokenModifiers.None),
//                R.EventName => (TokenKind.Event, TokenModifiers.None),

//                R.NamespaceName => (TokenKind.Namespace, TokenModifiers.None),
//                R.ParameterName => (TokenKind.Parameter, TokenModifiers.None),
//                R.LocalName => (TokenKind.Local, TokenModifiers.None),
//                R.LabelName => (TokenKind.Label, TokenModifiers.None),
//                R.ConstantName => (TokenKind.Constant, TokenModifiers.None),

//                R.Keyword => (TokenKind.Keyword, TokenModifiers.None),
//                R.Identifier => (TokenKind.Identifier, TokenModifiers.None),
//                R.StringLiteral => (TokenKind.StringLiteral, TokenModifiers.None),
//                R.NumericLiteral => (TokenKind.NumericLiteral, TokenModifiers.None),
//                R.CharacterLiteral => (TokenKind.CharacterLiteral, TokenModifiers.None),
//                R.Operator => (TokenKind.Operator, TokenModifiers.None),
//                R.Punctuation => (TokenKind.Punctuation, TokenModifiers.None),
//                R.Comment => (TokenKind.Comment, TokenModifiers.None),
//                R.PreprocessorKeyword => (TokenKind.Preprocessor, TokenModifiers.None),
//                R.ExcludedCode => (TokenKind.ExcludedCode, TokenModifiers.None),
//                R.Text => (TokenKind.Text, TokenModifiers.None),

//                _ => (TokenKind.Other, TokenModifiers.None),
//            };
//        }

//        /// Build a ClassifiedToken from a single classification string + span.
//        /// Useful when your pipeline already groups per-span.
//        public static ClassifiedToken FromSingle(string classification, int start, int length)
//        {
//            var (kind, mods) = MapClassification(classification);
//            return new ClassifiedToken(new TextSpan(start, length), kind, mods);
//        }

//        /// Merge multiple Roslyn classifications for the SAME span into one token.
//        /// (Roslyn often emits base + overlays; sometimes multiple bases – this resolves them.)
//        public static ClassifiedToken MergeForSpan(
//            IEnumerable<string> classificationsForSpan,
//            int start, int length)
//        {
//            TokenKind chosenBase = TokenKind.Other;
//            var modifiers = TokenModifiers.None;

//            foreach (var c in classificationsForSpan)
//            {
//                var (k, m) = MapClassification(c);
//                modifiers |= m;

//                // Prefer the highest priority base kind.
//                if (k != TokenKind.Other && HigherPriority(k, chosenBase))
//                    chosenBase = k;
//            }

//            return new ClassifiedToken(new TextSpan(start, length), chosenBase, modifiers);
//        }

//        private static bool HigherPriority(TokenKind a, TokenKind b)
//        {
//            int idxA = Array.IndexOf(BasePriority, a);
//            int idxB = Array.IndexOf(BasePriority, b);
//            if (idxA < 0) idxA = BasePriority.Length - 1;
//            if (idxB < 0) idxB = BasePriority.Length - 1;
//            return idxA < idxB;
//        }

//        /// Convenience: merge a batch of raw Roslyn tuples.
//        /// Input: many (classificationString, start, length) possibly overlapping per span.
//        public static IEnumerable<ClassifiedToken> MergeBatch(
//            IEnumerable<(string classification, int start, int length)> items)
//        {
//            return items
//                .GroupBy(it => (it.start, it.length))
//                .Select(g => MergeForSpan(g.Select(x => x.classification), g.Key.start, g.Key.length));
//        }
//    }
//}
