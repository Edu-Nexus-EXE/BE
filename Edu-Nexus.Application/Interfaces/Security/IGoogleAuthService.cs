using Edu_Nexus.Application.DTOs;

namespace Edu_Nexus.Application.Interfaces.Security;

public interface IGoogleAuthService
{
    Task<GoogleTokenPayloadDto?> VerifyTokenAsync(string idToken);
}
