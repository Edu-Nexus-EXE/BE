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
        var sepayTransactionId = payload.Id.ToString();

        _logger.LogInformation("SePay webhook: id={Id}, content={Content}, amount={Amount}",
            payload.Id, payload.Content, payload.TransferAmount);

        // 2. Chỉ xử lý tiền vào
        if (payload.TransferAmount <= 0)
        {
            _logger.LogInformation("SePay webhook: skipping outgoing transaction id={Id}", payload.Id);
            return true;
        }

        // 3. Dedup theo SePay transaction id — tránh complete 2 lần nếu SePay retry hoặc
        //    user vô tình CK 2 giao dịch riêng biệt cùng content.
        var alreadyProcessed = await _unitOfWork.PaymentOrders.FirstOrDefaultAsync(
            o => o.ProviderOrderId == sepayTransactionId, "", cancellationToken);
        if (alreadyProcessed != null)
        {
            _logger.LogInformation("SePay webhook: transaction {TxId} already linked to order {OrderId}, skipping",
                sepayTransactionId, alreadyProcessed.Id);
            return true;
        }

        // 4. Tìm PaymentOrder theo nội dung CK (EDUNEXUS XXXXXXXX)
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
            // Tiền đã vào TK nhưng không match được order nào → ghi WARN có đầy đủ
            // trường để admin tra cứu/refund thủ công. Vẫn trả 200 để tránh SePay retry storm.
            _logger.LogWarning(
                "SePay webhook UNRECONCILED: no matching order. txId={TxId}, account={Account}, amount={Amount}, content='{Content}', date={Date}",
                sepayTransactionId, payload.AccountNumber, payload.TransferAmount, content, payload.TransactionDate);
            return true;
        }

        // 5. Kiểm tra số tiền (không cho thanh toán thiếu)
        if (payload.TransferAmount < matchedOrder.Amount)
        {
            // Trả thiếu cũng cần ghi WARN có đầy đủ thông tin để admin reconcile.
            _logger.LogWarning(
                "SePay webhook UNDERPAID: order={OrderId} userId={UserId} expected={Expected} got={Got} txId={TxId} content='{Content}'",
                matchedOrder.Id, matchedOrder.UserId, matchedOrder.Amount, payload.TransferAmount,
                sepayTransactionId, content);
            return true;
        }

        // 6. Cập nhật PaymentOrder → Completed (lưu cả SePay tx id để truy vết + dedup)
        matchedOrder.Status = PaymentOrderStatus.Completed;
        matchedOrder.CompletedAt = DateTime.UtcNow;
        matchedOrder.ProviderOrderId = sepayTransactionId;
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
