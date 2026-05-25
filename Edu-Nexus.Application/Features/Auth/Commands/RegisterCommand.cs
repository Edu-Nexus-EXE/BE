using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.SubscriptionTiers;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using Edu_Nexus.Domain.Enums.Users;
using MediatR;

namespace Edu_Nexus.Application.Features.Auth.Commands;

public record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponseData>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseData>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public RegisterCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseData> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Request.Email && u.DeletedAt == null, "", cancellationToken);
        if (existingUser != null)
        {
            throw new Exception("409 EMAIL_EXISTS");
        }

        var hashedPassword = _passwordHasher.HashPassword(request.Request.Password);

        var baseSlug = Edu_Nexus.Application.Helpers.SlugHelper.GenerateSlug(request.Request.FullName);
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

        var user = new User
        {
            Email = request.Request.Email,
            PasswordHash = hashedPassword,
            FullName = request.Request.FullName,
            AuthProvider = AuthProvider.Email,
            Role = UserRole.User,
            IsSurveyCompleted = false,
            PortfolioUrlSlug = finalSlug
        };

        _unitOfWork.Users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
