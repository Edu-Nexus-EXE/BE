using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Enums.PaymentOrders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Jobs;

/// Cancels SePay payment_orders that have been "pending" longer than the
/// configured TTL. The VietQR a user scans is only valid for a few minutes
/// in practice, and leaving stale Pending rows around lets users continue
/// to display a dead order and confuses the webhook handler's
/// "unreconciled" classification on late transfers.
public class ExpirePendingPaymentOrdersJob
{
    private const int DefaultTtlMinutes = 30;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpirePendingPaymentOrdersJob> _logger;

    public ExpirePendingPaymentOrdersJob(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<ExpirePendingPaymentOrdersJob> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var ttlMinutes = _configuration.GetValue<int?>("SePay:OrderTtlMinutes") ?? DefaultTtlMinutes;
        var cutoff = DateTime.UtcNow.AddMinutes(-ttlMinutes);

        var stale = (await _unitOfWork.PaymentOrders.FindAsync(
            o => o.Status == PaymentOrderStatus.Pending
                 && o.PaymentProvider == PaymentProvider.SePay
                 && o.CreatedAt < cutoff,
            "", cancellationToken)).ToList();

        if (stale.Count == 0)
        {
            return;
        }

        foreach (var order in stale)
        {
            order.Status = PaymentOrderStatus.Cancelled;
            _unitOfWork.PaymentOrders.Update(order);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ExpirePendingPaymentOrdersJob: cancelled {Count} SePay orders older than {Ttl}m",
            stale.Count, ttlMinutes);
    }
}
