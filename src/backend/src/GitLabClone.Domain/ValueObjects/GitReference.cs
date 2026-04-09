namespace GitLabClone.Domain.ValueObjects;

/// <summary>
/// Represents a Git reference (branch name, tag, or commit SHA).
/// Defaults to "main" when not specified.
/// </summary>
public sealed record GitReference
{
    public string Value { get; }

    private GitReference(string value) => Value = value;

    public static GitReference Default => new("main");

    public static GitReference Create(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return Default;

        var trimmed = reference.Trim();

        if (trimmed.Length > 256)
            throw new ArgumentException("Git reference must not exceed 256 characters.", nameof(reference));

        return new GitReference(trimmed);
    }

    public override string ToString() => Value;

    public static implicit operator string(GitReference gitRef) => gitRef.Value;
}
