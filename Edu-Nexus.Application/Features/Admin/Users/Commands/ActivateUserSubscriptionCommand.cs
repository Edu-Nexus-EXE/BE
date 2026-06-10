using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Subscriptions;
using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.PaymentOrders;
using Edu_Nexus.Domain.Enums.SubscriptionTiers;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Users.Commands;

public record ActivateUserSubscriptionCommand(Guid UserId, ActivateUserSubscriptionRequest Request) : IRequest<AdminUserSubscriptionDto>;

public class ActivateUserSubscriptionCommandHandler : IRequestHandler<ActivateUserSubscriptionCommand, AdminUserSubscriptionDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public ActivateUserSubscriptionCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<AdminUserSubscriptionDto> Handle(ActivateUserSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        if (!SubscriptionDurations.IsValid(request.Request.DurationMonths))
        {
            throw new Exception("422 INVALID_DURATION");
        }

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Id == request.UserId && u.DeletedAt == null,
            nameof(User.UserSubscription),
            cancellationToken)
            ?? throw new Exception("404 USER_NOT_FOUND");

        var studentTier = await _unitOfWork.SubscriptionTiers.FirstOrDefaultAsync(
            t => t.TierCode == SubscriptionTierCode.Student, "", cancellationToken)
            ?? throw new Exception("404 STUDENT_TIER_NOT_FOUND");

        var now = DateTime.UtcNow;
        var extendFrom = user.UserSubscription?.ExpiresAt is { } existing && existing > now
            ? existing
            : now;
        var newExpiresAt = extendFrom.AddMonths(request.Request.DurationMonths);

        UserSubscription sub;
        if (user.UserSubscription == null)
        {
            sub = new UserSubscription
            {
                UserId = user.Id,
                TierId = studentTier.Id,
                Status = UserSubscriptionStatus.Active,
                StartedAt = now,
                ExpiresAt = newExpiresAt,
                AutoRenew = false,
            };
            _unitOfWork.UserSubscriptions.Add(sub);
        }
        else
        {
            sub = user.UserSubscription;
            sub.TierId = studentTier.Id;
            sub.Status = UserSubscriptionStatus.Active;
            sub.ExpiresAt = newExpiresAt;
            sub.CancelledAt = null;
            _unitOfWork.UserSubscriptions.Update(sub);
        }

        var amount = studentTier.PriceMonthly * request.Request.DurationMonths;
        var order = new PaymentOrder
        {
            UserId = user.Id,
            TierId = studentTier.Id,
            DurationMonths = request.Request.DurationMonths,
            Amount = amount,
            Currency = studentTier.Currency,
            PaymentProvider = PaymentProvider.ManualTransfer,
            ProviderOrderId = $"MANUAL_{now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}",
            Status = PaymentOrderStatus.Completed,
            CompletedAt = now,
        };
        _unitOfWork.PaymentOrders.Add(order);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        order.SubscriptionId = sub.Id;
        _unitOfWork.PaymentOrders.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            adminId,
            "activate_subscription",
            "user",
            user.Id,
            new
            {
                tierCode = studentTier.TierCode.ToString().ToLowerInvariant(),
                durationMonths = request.Request.DurationMonths,
                expiresAt = newExpiresAt,
                paymentOrderId = order.Id,
            },
            cancellationToken);

        return new AdminUserSubscriptionDto(
            studentTier.TierCode.ToString().ToLowerInvariant(),
            studentTier.DisplayName,
            sub.Status.ToString().ToLowerInvariant(),
            sub.StartedAt,
            sub.ExpiresAt,
            sub.AutoRenew);
    }
}
