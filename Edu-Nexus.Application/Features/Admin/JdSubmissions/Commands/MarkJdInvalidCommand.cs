using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.JdSubmissions.Commands;

public record MarkJdInvalidCommand(Guid JdId, MarkJdInvalidRequest? Request) : IRequest<Unit>;

public class MarkJdInvalidCommandHandler : IRequestHandler<MarkJdInvalidCommand, Unit>
{
    private const string AdminDismissedPrefix = "[ADMIN_DISMISSED]";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public MarkJdInvalidCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<Unit> Handle(MarkJdInvalidCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var jd = await _unitOfWork.JdSubmissions.FirstOrDefaultAsync(
            j => j.Id == request.JdId && j.DeletedAt == null, "", cancellationToken)
            ?? throw new Exception("404 JD_NOT_FOUND");

        var reason = string.IsNullOrWhiteSpace(request.Request?.Reason)
            ? null
            : request.Request!.Reason!.Trim();

        jd.DeletedAt = DateTime.UtcNow;
        jd.ParseError = reason != null
            ? $"{AdminDismissedPrefix} {reason}"
            : AdminDismissedPrefix;

        _unitOfWork.JdSubmissions.Update(jd);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            adminId,
            "mark_jd_invalid",
            "jd_submission",
            jd.Id,
            new { reason },
            cancellationToken);

        return Unit.Value;
    }
}
