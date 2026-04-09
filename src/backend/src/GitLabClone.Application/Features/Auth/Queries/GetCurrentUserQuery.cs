using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Auth.Queries;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public sealed record CurrentUserDto(
    Guid Id,
    string Username,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string Role
);

public sealed class GetCurrentUserQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Not authenticated.");

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        return new CurrentUserDto(
            user.Id, user.Username, user.Email,
            user.DisplayName, user.AvatarUrl, user.GlobalRole.ToString()
        );
    }
}
