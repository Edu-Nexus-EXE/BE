using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Security;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Edu_Nexus.Infrastructure.Security;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleTokenPayloadDto?> VerifyTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Google:ClientId"] }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return new GoogleTokenPayloadDto(payload.Email, payload.Name, payload.Subject, payload.Picture);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
