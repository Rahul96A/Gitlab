using System.Text;
using System.Text.RegularExpressions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Api.Middleware;

/// <summary>
/// ASP.NET Core middleware that intercepts Git Smart HTTP protocol requests.
///
/// URL pattern: /{owner}/{project-slug}.git/{operation}
/// where operation is one of:
///   - info/refs?service=git-upload-pack   (GET  — ref discovery for clone/fetch)
///   - info/refs?service=git-receive-pack  (GET  — ref discovery for push)
///   - git-upload-pack                     (POST — clone/fetch data transfer)
///   - git-receive-pack                    (POST — push data transfer)
///
/// This middleware runs BEFORE MVC routing so it can intercept these raw
/// Git protocol URLs that don't follow REST conventions.
///
/// Authorization logic:
/// - upload-pack (clone/fetch): Allowed for public repos without auth.
///   Private/internal repos require authentication and membership.
/// - receive-pack (push): Always requires authentication + Developer role or above.
/// </summary>
public sealed partial class GitSmartHttpMiddleware(
    RequestDelegate next,
    ILogger<GitSmartHttpMiddleware> logger
)
{
    // Matches: /{anything}/{slug}.git/info/refs  or  /{anything}/{slug}.git/git-upload-pack  etc.
    [GeneratedRegex(@"^/(?<owner>[a-z0-9_-]+)/(?<slug>[a-z0-9-]+)\.git/(?<operation>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex GitUrlPattern();

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (path is null || !path.Contains(".git/"))
        {
            await next(context);
            return;
        }

        var match = GitUrlPattern().Match(path);
        if (!match.Success)
        {
            await next(context);
            return;
        }

        var slug = match.Groups["slug"].Value;
        var operation = match.Groups["operation"].Value;

        // Resolve services from DI
        var db = context.RequestServices.GetRequiredService<IAppDbContext>();
        var gitHttpService = context.RequestServices.GetRequiredService<IGitHttpService>();

        // Look up the project by slug
        var project = await db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug, context.RequestAborted);

        if (project is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Repository not found.", context.RequestAborted);
            return;
        }

        // Determine the Git service type
        var isReceivePack = operation.Contains("receive-pack", StringComparison.OrdinalIgnoreCase);
        var isUploadPack = operation.Contains("upload-pack", StringComparison.OrdinalIgnoreCase);

        if (!isReceivePack && !isUploadPack && !operation.Equals("info/refs", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // For info/refs, get the service from query string
        if (operation.Equals("info/refs", StringComparison.OrdinalIgnoreCase))
        {
            var service = context.Request.Query["service"].ToString();
            isReceivePack = service == "git-receive-pack";
            isUploadPack = service == "git-upload-pack";
        }

        // ── Authorization ────────────────────────────────────────────────
        var currentUser = context.RequestServices.GetRequiredService<ICurrentUserService>();

        if (isReceivePack)
        {
            // Push always requires auth + Developer role
            if (!currentUser.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Basic realm=\"GitLabClone\"";
                return;
            }

            var hasAccess = await HasProjectAccessAsync(db, project.Id, currentUser.UserId!.Value, MemberRole.Developer);
            if (!hasAccess)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Push access denied.", context.RequestAborted);
                return;
            }
        }
        else if (isUploadPack)
        {
            // Clone/fetch: public repos are open, private/internal require auth
            if (project.Visibility == ProjectVisibility.Private)
            {
                if (!currentUser.IsAuthenticated)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers.WWWAuthenticate = "Basic realm=\"GitLabClone\"";
                    return;
                }

                var hasAccess = await HasProjectAccessAsync(db, project.Id, currentUser.UserId!.Value, MemberRole.Guest);
                if (!hasAccess)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }
            else if (project.Visibility == ProjectVisibility.Internal && !currentUser.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Basic realm=\"GitLabClone\"";
                return;
            }
        }

        // ── Execute Git operation ────────────────────────────────────────
        try
        {
            GitServiceResult result;

            if (operation.Equals("info/refs", StringComparison.OrdinalIgnoreCase))
            {
                var serviceName = context.Request.Query["service"].ToString();
                result = await gitHttpService.GetInfoRefsAsync(project.RepositoryPath, serviceName, context.RequestAborted);
            }
            else if (isUploadPack)
            {
                result = await gitHttpService.ExecuteUploadPackAsync(project.RepositoryPath, context.Request.Body, context.RequestAborted);
            }
            else // receive-pack
            {
                result = await gitHttpService.ExecuteReceivePackAsync(project.RepositoryPath, context.Request.Body, context.RequestAborted);
            }

            context.Response.StatusCode = result.StatusCode;
            context.Response.ContentType = result.ContentType;
            context.Response.Headers.CacheControl = "no-cache";

            await result.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Git operation failed for {Slug}/{Operation}", slug, operation);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Git operation failed.", context.RequestAborted);
        }
    }

    /// <summary>
    /// Checks if a user has at least the specified role on a project.
    /// Also returns true if the user is a global Admin.
    /// </summary>
    private static async Task<bool> HasProjectAccessAsync(
        IAppDbContext db, Guid projectId, Guid userId, MemberRole minimumRole)
    {
        // Check global admin
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.GlobalRole == MemberRole.Admin)
            return true;

        // Check project membership
        var membership = await db.ProjectMembers.AsNoTracking()
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

        return membership is not null && membership.Role >= minimumRole;
    }
}
