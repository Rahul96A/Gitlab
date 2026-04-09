namespace GitLabClone.Application.Common.Exceptions;

public sealed class ForbiddenException(string? message = null)
    : Exception(message ?? "You do not have permission to perform this action.");
