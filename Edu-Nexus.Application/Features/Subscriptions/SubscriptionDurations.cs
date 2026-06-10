using System.Linq;

namespace Edu_Nexus.Application.Features.Subscriptions;

/// <summary>
/// Thời hạn gói hợp lệ — phải khớp DB constraint
/// `payment_orders_duration_months_check CHECK (duration_months IN (1,3,6))`
/// và Requirement FR8 (admin activate: 1 tháng / 3 tháng / 6 tháng).
/// DB-first: DB là nguồn chân lý, app validate theo đúng tập này.
/// </summary>
public static class SubscriptionDurations
{
    public static readonly IReadOnlyList<int> Allowed = new[] { 1, 3, 6 };

    public static bool IsValid(int months) => Allowed.Contains(months);
}
