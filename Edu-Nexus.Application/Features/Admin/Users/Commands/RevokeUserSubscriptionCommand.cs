using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Users.Commands;

public record RevokeUserSubscriptionCommand(Guid UserId) : IRequest<Unit>;

public class RevokeUserSubscriptionCommandHandler : IRequestHandler<RevokeUserSubscriptionCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public RevokeUserSubscriptionCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<Unit> Handle(RevokeUserSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Id == request.UserId && u.DeletedAt == null,
            nameof(User.UserSubscription),
            cancellationToken)
            ?? throw new Exception("404 USER_NOT_FOUND");

        if (user.UserSubscription == null || user.UserSubscription.Status != UserSubscriptionStatus.Active)
        {
            throw new Exception("422 NO_ACTIVE_SUBSCRIPTION");
        }

        var sub = user.UserSubscription;
        var prevTierId = sub.TierId;
        var prevExpiresAt = sub.ExpiresAt;

        sub.Status = UserSubscriptionStatus.Cancelled;
        sub.CancelledAt = DateTime.UtcNow;
        sub.AutoRenew = false;
        _unitOfWork.UserSubscriptions.Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            adminId,
            "revoke_subscription",
            "user",
            user.Id,
            new
            {
                previousTierId = prevTierId,
                previousExpiresAt = prevExpiresAt,
                cancelledAt = sub.CancelledAt,
            },
            cancellationToken);

        return Unit.Value;
    }
}
