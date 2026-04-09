using System.Security.Claims;
using System.Text;
using GitLabClone.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Api.Middleware;

/// <summary>
/// Handles HTTP Basic Authentication for Git clients.
///
/// Git clients (CLI, IDE integrations) send credentials via the
/// Authorization: Basic header. This middleware intercepts requests
/// to .git/ URLs, validates credentials against the database, and
/// sets the ClaimsPrincipal so downstream middleware (GitSmartHttp)
/// can check authorization.
///
/// This runs BEFORE the JWT middleware and only activates for Git URLs.
/// Non-git requests pass through untouched to the JWT handler.
/// </summary>
public sealed class GitBasicAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        // Only intercept .git/ requests
        if (path is not null && path.Contains(".git/") &&
            context.Request.Headers.Authorization.ToString() is { Length: > 0 } authHeader &&
            authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encoded = authHeader["Basic ".Length..].Trim();
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var separatorIndex = decoded.IndexOf(':');

                if (separatorIndex > 0)
                {
                    var username = decoded[..separatorIndex].ToLowerInvariant();
                    var password = decoded[(separatorIndex + 1)..];

                    var db = context.RequestServices.GetRequiredService<IAppDbContext>();
                    var user = await db.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

                    if (user is not null && user.IsActive &&
                        BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    {
                        // Set the identity for downstream middleware
                        var claims = new List<Claim>
                        {
                            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                            new(ClaimTypes.Name, user.Username),
                            new(ClaimTypes.Email, user.Email),
                            new(ClaimTypes.Role, user.GlobalRole.ToString())
                        };

                        var identity = new ClaimsIdentity(claims, "BasicAuth");
                        context.User = new ClaimsPrincipal(identity);
                    }
                }
            }
            catch
            {
                // Invalid base64 or format — ignore, let downstream handle 401
            }
        }

        await next(context);
    }
}
