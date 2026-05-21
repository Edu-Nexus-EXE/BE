using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.AssessmentPaths;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;

namespace Edu_Nexus.Application.Features.AssessmentPaths.Commands;

public record CreateAssessmentPathCommand(Guid JdId, CreateAssessmentPathRequest Request) : IRequest<AssessmentPathDto>;

public class CreateAssessmentPathCommandHandler : IRequestHandler<CreateAssessmentPathCommand, AssessmentPathDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateAssessmentPathCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<AssessmentPathDto> Handle(CreateAssessmentPathCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var pathType = ParsePathType(request.Request.PathType);

        var jd = await _unitOfWork.JdSubmissions
            .FirstOrDefaultAsync(j => j.Id == request.JdId && j.UserId == userId && j.DeletedAt == null, "", cancellationToken)
            ?? throw new Exception("404 JD_NOT_FOUND");

        var existing = await _unitOfWork.AssessmentPaths
            .FirstOrDefaultAsync(p => p.JdId == request.JdId, "", cancellationToken);

        if (existing != null)
        {
            throw new Exception("409 PATH_ALREADY_EXISTS");
        }

        if (pathType == PathType.Assessment)
        {
            await EnforceAssessmentQuotaAsync(userId, cancellationToken);
        }

        var path = new AssessmentPath
        {
            JdId = request.JdId,
            UserId = userId,
            PathType = pathType,
        };

        _unitOfWork.AssessmentPaths.Add(path);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AssessmentPathDto(
            path.Id,
            path.JdId,
            path.PathType.ToString().ToLowerInvariant(),
            path.CreatedAt == default ? DateTime.UtcNow : path.CreatedAt);
    }

    private static PathType ParsePathType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) throw new Exception("422 INVALID_PATH_TYPE");
        return raw.Trim().ToLowerInvariant() switch
        {
            "cv" => PathType.Cv,
            "assessment" => PathType.Assessment,
            _ => throw new Exception("422 INVALID_PATH_TYPE")
        };
    }

    private async Task EnforceAssessmentQuotaAsync(Guid userId, CancellationToken ct)
    {
        var subscription = await _unitOfWork.UserSubscriptions
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active,
                includeProperties: nameof(UserSubscription.Tier),
                cancellationToken: ct);

        var assessmentQuota = subscription?.Tier?.AssessmentQuota ?? 3;
        if (assessmentQuota < 0) return;

        var current = (await _unitOfWork.AssessmentPaths.FindAsync(
            p => p.UserId == userId && p.PathType == PathType.Assessment,
            "", ct)).Count();

        if (current >= assessmentQuota)
        {
            throw new Exception($"403 QUOTA_EXCEEDED|assessment|{current}|{assessmentQuota}");
        }
    }
}
