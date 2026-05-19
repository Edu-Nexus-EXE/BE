using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using MediatR;

namespace Edu_Nexus.Application.Features.Auth.Queries;

public record LoginQuery(LoginRequest Request) : IRequest<AuthResponseData>;

public class LoginQueryHandler : IRequestHandler<LoginQuery, AuthResponseData>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginQueryHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseData> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Request.Email && u.DeletedAt == null, "", cancellationToken);
        
        if (user == null || user.PasswordHash == null || !_passwordHasher.VerifyPassword(request.Request.Password, user.PasswordHash))
        {
            throw new Exception("401 INVALID_CREDENTIALS");
        }

        if (user.IsBanned)
        {
            throw new Exception("403 ACCOUNT_BANNED");
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
