using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Enums.JdSubmissions;
using Edu_Nexus.Domain.Enums.Roadmaps;
using MediatR;

namespace Edu_Nexus.Application.Features.JdSubmissions.Queries;

public record GetUserJdsQuery(int Page, int PageSize, string? Status) : IRequest<PagedResult<JdSubmissionListItemDto>>;

public class GetUserJdsQueryHandler : IRequestHandler<GetUserJdsQuery, PagedResult<JdSubmissionListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetUserJdsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<JdSubmissionListItemDto>> Handle(GetUserJdsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 10 : request.PageSize;

        ParseStatus? statusFilter = request.Status?.ToLowerInvariant() switch
        {
            "pending" => ParseStatus.Pending,
            "processing" => ParseStatus.Processing,
            "completed" => ParseStatus.Completed,
            "failed" => ParseStatus.Failed,
            null or "" => null,
            _ => throw new Exception("422 INVALID_STATUS_FILTER")
        };

        // KHÔNG Include GapAnalysis/Roadmap: chúng là quan hệ 1-N (gap có nhiều version,
        // 1 JD có nhiều roadmap active/archived/failed). Include reference đơn lên quan hệ 1-N
        // khiến EF fan-out — JD bị lặp trong list và cờ has* trỏ vào 1 bản tuỳ ý. Lấy JD "trần"
        // (không Include) để pagination/đếm chính xác, rồi tính cờ bằng existence query theo trang.
        var all = (await _unitOfWork.JdSubmissions.FindAsync(
            j => j.UserId == userId
                && j.DeletedAt == null
                && (statusFilter == null || j.ParseStatus == statusFilter),
            "",
            cancellationToken))
            .OrderByDescending(j => j.CreatedAt)
            .ToList();

        var totalItems = all.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var pageEntities = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pageIds = pageEntities.Select(j => j.Id).ToList();

        var pathJdIds = new HashSet<Guid>();
        var gapJdIds = new HashSet<Guid>();
        var activeRoadmapJdIds = new HashSet<Guid>();

        if (pageIds.Count > 0)
        {
            pathJdIds = (await _unitOfWork.AssessmentPaths.FindAsync(
                p => pageIds.Contains(p.JdId), "", cancellationToken))
                .Select(p => p.JdId).ToHashSet();

            gapJdIds = (await _unitOfWork.GapAnalyses.FindAsync(
                g => pageIds.Contains(g.JdId), "", cancellationToken))
                .Select(g => g.JdId).ToHashSet();

            activeRoadmapJdIds = (await _unitOfWork.Roadmaps.FindAsync(
                r => pageIds.Contains(r.JdId) && r.Status == RoadmapStatus.Active, "", cancellationToken))
                .Select(r => r.JdId).ToHashSet();
        }

        var items = pageEntities
            .Select(j => new JdSubmissionListItemDto(
                j.Id,
                j.SourceType.ToString().ToLowerInvariant(),
                j.JobTitle,
                j.JobRoleCategory,
                j.SeniorityLevel,
                j.ParseStatus.ToString().ToLowerInvariant(),
                j.CreatedAt,
                HasAssessmentPath: pathJdIds.Contains(j.Id),
                HasGapAnalysis: gapJdIds.Contains(j.Id),
                HasActiveRoadmap: activeRoadmapJdIds.Contains(j.Id)))
            .ToList();

        return new PagedResult<JdSubmissionListItemDto>(
            items,
            new PaginationDto(page, pageSize, totalItems, totalPages));
    }
}
