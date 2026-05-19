using Edu_Nexus.Domain.Entities;

namespace Edu_Nexus.Application.Interfaces.Security;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
}
