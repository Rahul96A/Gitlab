using FluentValidation;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Domain.Entities;
using GitLabClone.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Auth.Commands;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string DisplayName
) : IRequest<AuthResponse>;

public sealed record AuthResponse(
    Guid UserId,
    string Username,
    string Email,
    string DisplayName,
    string Token,
    string Role
);

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().MinimumLength(3).MaximumLength(39)
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Username may only contain alphanumeric characters, hyphens, and underscores.");

        RuleFor(x => x.Email).NotEmpty().EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public sealed class RegisterCommandHandler(
    IAppDbContext db,
    IJwtTokenService jwtService
) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check uniqueness
        var exists = await db.Users.AnyAsync(
            u => u.Username == request.Username || u.Email == request.Email,
            cancellationToken
        );

        if (exists)
            throw new Common.Exceptions.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Username", "Username or email already taken.")]
            );

        var user = new User
        {
            Username = request.Username.ToLowerInvariant(),
            Email = request.Email.ToLowerInvariant(),
            DisplayName = request.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            GlobalRole = MemberRole.Developer
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var token = jwtService.GenerateToken(user);

        return new AuthResponse(user.Id, user.Username, user.Email, user.DisplayName, token, user.GlobalRole.ToString());
    }
}
