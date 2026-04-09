using GitLabClone.Domain.Entities;

namespace GitLabClone.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
}
