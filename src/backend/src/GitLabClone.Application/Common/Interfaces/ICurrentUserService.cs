namespace GitLabClone.Application.Common.Interfaces;

/// <summary>
/// Provides the identity of the currently authenticated user.
/// Implemented in Infrastructure by reading HttpContext claims.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
}
