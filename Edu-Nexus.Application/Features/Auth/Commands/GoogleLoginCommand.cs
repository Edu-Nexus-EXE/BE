using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.SubscriptionTiers;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using Edu_Nexus.Domain.Enums.Users;
using MediatR;

namespace Edu_Nexus.Application.Features.Auth.Commands;

public record GoogleLoginCommand(GoogleLoginRequest Request) : IRequest<AuthResponseData>;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, AuthResponseData>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly ITokenService _tokenService;

    public GoogleLoginCommandHandler(IUnitOfWork unitOfWork, IGoogleAuthService googleAuthService, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _googleAuthService = googleAuthService;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseData> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var payload = await _googleAuthService.VerifyTokenAsync(request.Request.IdToken);
        if (payload == null)
        {
            throw new Exception("401 INVALID_GOOGLE_TOKEN");
        }

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.GoogleSub == payload.Subject || u.Email == payload.Email, "", cancellationToken);

        if (user == null)
        {
            var fullName = payload.Name ?? payload.Email;
            var baseSlug = Edu_Nexus.Application.Helpers.SlugHelper.GenerateSlug(fullName);
            var finalSlug = baseSlug;
            bool isUnique = false;
            
            while (!isUnique)
            {
                var exists = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.PortfolioUrlSlug == finalSlug, "", cancellationToken);
                if (exists == null)
                {
                    isUnique = true;
                }
                else
                {
                    finalSlug = $"{baseSlug}-{Guid.NewGuid().ToString("N")[..4]}";
                }
            }

            user = new User
            {
                Email = payload.Email,
                FullName = fullName,
                GoogleSub = payload.Subject,
                AvatarUrl = payload.Picture,
                AuthProvider = AuthProvider.Google,
                Role = UserRole.User,
                IsSurveyCompleted = false,
                PortfolioUrlSlug = finalSlug
            };
            
            _unitOfWork.Users.Add(user);
        }
        else
        {
            if (user.IsBanned)
            {
                throw new Exception("403 ACCOUNT_BANNED");
            }
            if (user.GoogleSub == null)
            {
                user.GoogleSub = payload.Subject;
            }
            if (user.AvatarUrl == null && !string.IsNullOrEmpty(payload.Picture))
            {
                user.AvatarUrl = payload.Picture;
            }
        }

        user.LastLoginAt = DateTime.UtcNow;

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenStr = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashRefreshToken(refreshTokenStr);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _unitOfWork.RefreshTokens.Add(refreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseData(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString().ToLower(),
            user.IsSurveyCompleted,
            accessToken,
            refreshTokenStr
        );
    }
}
