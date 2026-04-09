using System.Text.RegularExpressions;

namespace GitLabClone.Domain.ValueObjects;

/// <summary>
/// URL-safe identifier for projects/namespaces. Enforces the same rules as GitLab:
/// lowercase alphanumeric + hyphens, no leading/trailing hyphens, 2-64 chars.
/// Implemented as a record for value equality semantics.
/// </summary>
public sealed partial record Slug
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug Create(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        normalized = InvalidCharsRegex().Replace(normalized, "-");
        normalized = normalized.Trim('-');

        if (normalized.Length < 2 || normalized.Length > 64)
            throw new ArgumentException("Slug must be between 2 and 64 characters.", nameof(input));

        if (!ValidSlugRegex().IsMatch(normalized))
            throw new ArgumentException("Slug contains invalid characters.", nameof(input));

        return new Slug(normalized);
    }

    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;

    [GeneratedRegex(@"[^a-z0-9-]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")]
    private static partial Regex ValidSlugRegex();
}
