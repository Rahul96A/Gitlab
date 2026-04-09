namespace GitLabClone.Domain.Enums;

/// <summary>
/// Maps to GitLab's permission model. Numeric values represent access level
/// (higher = more permissions). Used in authorization policies.
/// </summary>
public enum MemberRole
{
    Guest = 10,
    Reporter = 20,
    Developer = 30,
    Maintainer = 40,
    Admin = 50
}
