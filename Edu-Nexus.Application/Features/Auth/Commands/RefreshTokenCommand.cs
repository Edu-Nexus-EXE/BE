using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using MediatR;

namespace Edu_Nexus.Application.Features.Auth.Commands;

public record RefreshTokenCommand(TokenRefreshRequest Request) : IRequest<TokenRefreshResponseData>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenRefreshResponseData>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<TokenRefreshResponseData> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hashedInputToken = _tokenService.HashRefreshToken(request.Request.RefreshToken);

        var existingToken = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hashedInputToken, "User", cancellationToken);

        if (existingToken == null || existingToken.RevokedAt != null || existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new Exception("401 INVALID_TOKEN");
        }

        if (existingToken.User.IsBanned)
        {
            throw new Exception("403 ACCOUNT_BANNED");
        }

        existingToken.RevokedAt = DateTime.UtcNow;

        var user = existingToken.User;
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshTokenStr = _tokenService.GenerateRefreshToken();
        var hashedNewRefreshToken = _tokenService.HashRefreshToken(newRefreshTokenStr);

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hashedNewRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _unitOfWork.RefreshTokens.Add(newRefreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TokenRefreshResponseData(newAccessToken, newRefreshTokenStr);
    }
}
