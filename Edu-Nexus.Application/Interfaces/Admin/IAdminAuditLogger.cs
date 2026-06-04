namespace Edu_Nexus.Application.Interfaces.Admin;

public interface IAdminAuditLogger
{
    Task LogAsync(
        Guid adminUserId,
        string actionType,
        string targetType,
        Guid targetId,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}
