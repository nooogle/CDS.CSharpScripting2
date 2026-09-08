using System;

namespace CDS.CSharpScript2.CodeCompletion;

/// <summary>
/// The result of testing one completion item against the text the user has typed.
/// </summary>
public readonly struct CompletionMatch : IEquatable<CompletionMatch>
{
    /// <summary>A match result meaning the item should not be offered.</summary>
    public static readonly CompletionMatch NoMatch = new CompletionMatch(CompletionMatchKind.None, -1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CompletionMatch"/> struct.
    /// </summary>
    /// <param name="kind">How well the item matched.</param>
    /// <param name="index">The offset within the item at which the match begins, or -1 when there is no match.</param>
    public CompletionMatch(CompletionMatchKind kind, int index)
    {
        Kind = kind;
        Index = index;
    }

    /// <summary>Gets how well the item matched.</summary>
    public CompletionMatchKind Kind { get; }

    /// <summary>
    /// Gets the offset within the item at which the match begins, or -1 when there is no match.
    /// Earlier matches are the better ones, so this breaks ties within a <see cref="Kind"/>.
    /// </summary>
    public int Index { get; }

    /// <summary>Gets a value indicating whether the item should be offered at all.</summary>
    public bool IsMatch => Kind != CompletionMatchKind.None;

    /// <summary>
    /// Ranks this match against another: negative when this one is the better match, positive when
    /// <paramref name="other"/> is, zero when the two are indistinguishable.
    /// </summary>
    /// <param name="other">The match to compare against.</param>
    /// <returns>A value indicating the relative quality of the two matches.</returns>
    public int CompareQualityTo(CompletionMatch other)
    {
        if (Kind != other.Kind)
        {
            return other.Kind.CompareTo(Kind);
        }

        return Index.CompareTo(other.Index);
    }

    /// <inheritdoc/>
    public bool Equals(CompletionMatch other) => Kind == other.Kind && Index == other.Index;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CompletionMatch other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => ((int)Kind * 397) ^ Index;

    /// <summary>Determines whether two matches are equal.</summary>
    /// <param name="left">The first match.</param>
    /// <param name="right">The second match.</param>
    /// <returns><see langword="true"/> when the two are equal.</returns>
    public static bool operator ==(CompletionMatch left, CompletionMatch right) => left.Equals(right);

    /// <summary>Determines whether two matches differ.</summary>
    /// <param name="left">The first match.</param>
    /// <param name="right">The second match.</param>
    /// <returns><see langword="true"/> when the two differ.</returns>
    public static bool operator !=(CompletionMatch left, CompletionMatch right) => !left.Equals(right);
}
