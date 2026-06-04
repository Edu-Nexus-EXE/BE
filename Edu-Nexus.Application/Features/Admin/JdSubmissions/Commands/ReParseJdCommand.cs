using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Enums.JdSubmissions;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.JdSubmissions.Commands;

public record ReParseJdCommand(Guid JdId) : IRequest<Unit>;

public class ReParseJdCommandHandler : IRequestHandler<ReParseJdCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJdParseQueue _queue;
    private readonly IAdminAuditLogger _audit;

    public ReParseJdCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IJdParseQueue queue,
        IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _queue = queue;
        _audit = audit;
    }

    public async Task<Unit> Handle(ReParseJdCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var jd = await _unitOfWork.JdSubmissions.FirstOrDefaultAsync(
            j => j.Id == request.JdId && j.DeletedAt == null, "", cancellationToken)
            ?? throw new Exception("404 JD_NOT_FOUND");

        if (jd.ParseStatus == ParseStatus.Processing)
        {
            throw new Exception("422 ALREADY_PROCESSING");
        }

        var previousStatus = jd.ParseStatus;
        var previousError = jd.ParseError;

        jd.ParseStatus = ParseStatus.Pending;
        jd.ParseError = null;
        _unitOfWork.JdSubmissions.Update(jd);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _queue.Enqueue(jd.Id);

        await _audit.LogAsync(
            adminId,
            "reparse_jd",
            "jd_submission",
            jd.Id,
            new
            {
                previousStatus = previousStatus.ToString().ToLowerInvariant(),
                previousError,
            },
            cancellationToken);

        return Unit.Value;
    }
}
