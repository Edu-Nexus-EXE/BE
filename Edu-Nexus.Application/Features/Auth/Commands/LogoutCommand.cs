using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;

namespace Edu_Nexus.Application.Features.Auth.Commands;

public record LogoutCommand(LogoutRequest Request) : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public LogoutCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hashedToken = _tokenService.HashRefreshToken(request.Request.RefreshToken);

        var token = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken, "", cancellationToken);
        
        if (token != null && token.RevokedAt == null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
