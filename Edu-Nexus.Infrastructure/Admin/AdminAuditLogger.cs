using System.Net;
using System.Text.Json;
using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Edu_Nexus.Infrastructure.Admin;

public class AdminAuditLogger : IAdminAuditLogger
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminAuditLogger(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        Guid adminUserId,
        string actionType,
        string targetType,
        Guid targetId,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var action = new AdminAction
        {
            AdminUserId = adminUserId,
            ActionType = actionType,
            TargetType = targetType,
            TargetId = targetId,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            IpAddress = ResolveRemoteIp(),
        };

        _unitOfWork.AdminActions.Add(action);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private IPAddress? ResolveRemoteIp()
    {
        var ctx = _httpContextAccessor.HttpContext;
        return ctx?.Connection?.RemoteIpAddress;
    }
}
