using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Users.Commands;

public record SetUserBanCommand(Guid UserId, SetUserBanRequest Request) : IRequest<Unit>;

public class SetUserBanCommandHandler : IRequestHandler<SetUserBanCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public SetUserBanCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<Unit> Handle(SetUserBanCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Id == request.UserId && u.DeletedAt == null, "", cancellationToken)
            ?? throw new Exception("404 USER_NOT_FOUND");

        if (user.Id == adminId)
        {
            throw new Exception("422 CANNOT_BAN_SELF");
        }

        if (user.IsBanned == request.Request.IsBanned)
        {
            return Unit.Value;
        }

        user.IsBanned = request.Request.IsBanned;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            adminId,
            request.Request.IsBanned ? "ban_user" : "unban_user",
            "user",
            user.Id,
            new { isBanned = request.Request.IsBanned },
            cancellationToken);

        return Unit.Value;
    }
}
