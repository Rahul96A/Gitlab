using FluentValidation;
using GitLabClone.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Auth.Commands;

public sealed record LoginCommand(string UsernameOrEmail, string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmail).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IAppDbContext db,
    IJwtTokenService jwtService
) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var identifier = request.UsernameOrEmail.ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Username == identifier || u.Email == identifier,
            cancellationToken
        );

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Common.Exceptions.ForbiddenException("Invalid credentials.");

        var token = jwtService.GenerateToken(user);

        return new AuthResponse(user.Id, user.Username, user.Email, user.DisplayName, token, user.GlobalRole.ToString());
    }
}
