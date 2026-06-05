using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Configuration;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.PaymentOrders;
using Edu_Nexus.Domain.Enums.SubscriptionTiers;
using MediatR;

namespace Edu_Nexus.Application.Features.Subscriptions.Commands;

public record CreatePaymentOrderCommand(CreateOrderRequest Request) : IRequest<CreateOrderResponse>;

public class CreatePaymentOrderCommandHandler : IRequestHandler<CreatePaymentOrderCommand, CreateOrderResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISePaySettings _sePaySettings;

    public CreatePaymentOrderCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ISePaySettings sePaySettings)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _sePaySettings = sePaySettings;
    }

    public async Task<CreateOrderResponse> Handle(CreatePaymentOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var req = request.Request;

        if (req.DurationMonths <= 0 || req.DurationMonths > 12)
            throw new Exception("422 INVALID_DURATION");

        if (!Enum.TryParse<SubscriptionTierCode>(req.TierCode, true, out var tierCode))
            throw new Exception("422 INVALID_TIER_CODE");

        var tier = await _unitOfWork.SubscriptionTiers.FirstOrDefaultAsync(
            t => t.TierCode == tierCode && t.IsActive, "", cancellationToken)
            ?? throw new Exception("404 TIER_NOT_FOUND");

        if (tier.PriceMonthly == 0)
            throw new Exception("422 CANNOT_PAY_FREE_TIER");

        var amount = tier.PriceMonthly * req.DurationMonths;

        var order = new PaymentOrder
        {
            UserId = userId,
            TierId = tier.Id,
            DurationMonths = req.DurationMonths,
            Amount = amount,
            Currency = tier.Currency,
            PaymentProvider = PaymentProvider.SePay,
            Status = PaymentOrderStatus.Pending,
        };

        _unitOfWork.PaymentOrders.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Sinh mã chuyển khoản: EDUNEXUS + 8 ký tự đầu OrderId
        var shortCode = order.Id.ToString("N")[..8].ToUpper();
        var transferContent = $"EDUNEXUS {shortCode}";

        var bankAccount = _sePaySettings.BankAccount;
        var bankCode = _sePaySettings.BankCode;
        var accountName = _sePaySettings.AccountName;

        // VietQR URL (public API, không cần auth)
        var amountInt = (long)amount;
        var qrUrl = $"https://img.vietqr.io/image/{bankCode}-{bankAccount}-compact2.png" +
                    $"?amount={amountInt}&addInfo={Uri.EscapeDataString(transferContent)}&accountName={Uri.EscapeDataString(accountName)}";

        return new CreateOrderResponse(
            order.Id, amount, tier.Currency,
            transferContent, bankAccount, bankCode, accountName,
            qrUrl, "pending");
    }
}
