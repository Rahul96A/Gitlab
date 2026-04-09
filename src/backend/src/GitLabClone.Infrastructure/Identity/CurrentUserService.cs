using System.Security.Claims;
using GitLabClone.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GitLabClone.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Username => User?.FindFirstValue(ClaimTypes.Name);

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];
}
