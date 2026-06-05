using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Configuration;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.PaymentOrders;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Application.Features.Subscriptions.Commands;

public record HandleSepayWebhookCommand(SepayWebhookPayload Payload, string AuthorizationHeader) : IRequest<bool>;

public class HandleSepayWebhookCommandHandler : IRequestHandler<HandleSepayWebhookCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISePaySettings _sePaySettings;
    private readonly ILogger<HandleSepayWebhookCommandHandler> _logger;

    public HandleSepayWebhookCommandHandler(
        IUnitOfWork unitOfWork,
        ISePaySettings sePaySettings,
        ILogger<HandleSepayWebhookCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _sePaySettings = sePaySettings;
        _logger = logger;
    }

    public async Task<bool> Handle(HandleSepayWebhookCommand request, CancellationToken cancellationToken)
    {
        // 1. Xác thực API Key
        var expectedKey = NormalizeApiKey(_sePaySettings.ApiKey);
        var clientKey = NormalizeApiKey(request.AuthorizationHeader);
        if (string.IsNullOrWhiteSpace(expectedKey) ||
            !string.Equals(clientKey, expectedKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("SePay webhook: unauthorized - invalid API key");
            throw new Exception("401 UNAUTHORIZED");
        }

        var payload = request.Payload;
        _logger.LogInformation("SePay webhook: id={Id}, content={Content}, amount={Amount}",
            payload.Id, payload.Content, payload.TransferAmount);

        // 2. Chỉ xử lý tiền vào
        if (payload.TransferAmount <= 0)
        {
            _logger.LogInformation("SePay webhook: skipping outgoing transaction");
            return true;
        }

        // 3. Tìm PaymentOrder theo nội dung CK (EDUNEXUS XXXXXXXX)
        var content = payload.Content ?? "";
        PaymentOrder? matchedOrder = null;

        var pendingOrders = await _unitOfWork.PaymentOrders.FindAsync(
            o => o.Status == PaymentOrderStatus.Pending &&
                 o.PaymentProvider == PaymentProvider.SePay,
            "Tier", cancellationToken);

        foreach (var order in pendingOrders)
        {
            var shortCode = order.Id.ToString("N")[..8].ToUpper();
            if (content.Contains($"EDUNEXUS {shortCode}", StringComparison.OrdinalIgnoreCase))
            {
                matchedOrder = order;
                break;
            }
        }

        if (matchedOrder == null)
        {
            _logger.LogInformation("SePay webhook: no matching order for content='{Content}'", content);
            return true; // Trả 200 tránh SePay retry
        }

        // 4. Idempotency
        if (matchedOrder.Status == PaymentOrderStatus.Completed)
            return true;

        // 5. Kiểm tra số tiền (không cho thanh toán thiếu)
        if (payload.TransferAmount < matchedOrder.Amount)
        {
            _logger.LogWarning("SePay webhook: underpaid order {OrderId}. Expected={E}, Got={G}",
                matchedOrder.Id, matchedOrder.Amount, payload.TransferAmount);
            return true;
        }

        // 6. Cập nhật PaymentOrder → Completed
        matchedOrder.Status = PaymentOrderStatus.Completed;
        matchedOrder.CompletedAt = DateTime.UtcNow;
        _unitOfWork.PaymentOrders.Update(matchedOrder);

        // 7. Kích hoạt / gia hạn UserSubscription
        var existing = await _unitOfWork.UserSubscriptions.FirstOrDefaultAsync(
            s => s.UserId == matchedOrder.UserId,
            "", cancellationToken);

        var now = DateTime.UtcNow;
        if (existing != null)
        {
            var wasActive = existing.Status == UserSubscriptionStatus.Active;
            var baseDate = (wasActive && existing.ExpiresAt > now)
                ? existing.ExpiresAt.Value
                : now;

            existing.TierId = matchedOrder.TierId;
            existing.ExpiresAt = baseDate.AddMonths(matchedOrder.DurationMonths);
            existing.UpdatedAt = now;
            existing.Status = UserSubscriptionStatus.Active;
            existing.CancelledAt = null;
            if (!wasActive)
            {
                existing.StartedAt = now;
            }

            matchedOrder.SubscriptionId = existing.Id;
            _unitOfWork.UserSubscriptions.Update(existing);
        }
        else
        {
            var newSub = new UserSubscription
            {
                UserId = matchedOrder.UserId,
                TierId = matchedOrder.TierId,
                Status = UserSubscriptionStatus.Active,
                StartedAt = now,
                ExpiresAt = now.AddMonths(matchedOrder.DurationMonths),
                AutoRenew = false,
            };
            _unitOfWork.UserSubscriptions.Add(newSub);
            matchedOrder.SubscriptionId = newSub.Id;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SePay webhook: order {OrderId} completed, subscription activated for user {UserId}",
            matchedOrder.Id, matchedOrder.UserId);

        return true;
    }

    private static string NormalizeApiKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        key = key.Trim();
        if (key.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            key = key[7..].Trim();
        }
        else if (key.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase))
        {
            key = key[7..].Trim();
        }
        return key;
    }
}
