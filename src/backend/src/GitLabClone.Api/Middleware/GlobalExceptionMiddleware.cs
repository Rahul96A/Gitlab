using System.Net;
using System.Text.Json;
using GitLabClone.Application.Common.Exceptions;

namespace GitLabClone.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and maps them to standard HTTP problem responses.
/// Application-layer exceptions map to specific HTTP status codes;
/// unexpected exceptions become 500s with details hidden in production.
/// </summary>
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment
)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (HttpStatusCode.BadRequest, "Validation Error", ve.Errors as object),
            NotFoundException => (HttpStatusCode.NotFound, "Not Found", null),
            ForbiddenException => (HttpStatusCode.Forbidden, "Forbidden", null),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized", null),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error", null)
        };

        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new
        {
            type = $"https://httpstatuses.io/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = environment.IsDevelopment() ? exception.Message : title,
            errors,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions));
    }
}
