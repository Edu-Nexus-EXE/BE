using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
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

        var includes = $"{nameof(JdSubmission.AssessmentPath)},{nameof(JdSubmission.GapAnalysis)},{nameof(JdSubmission.Roadmap)}";

        var all = (await _unitOfWork.JdSubmissions.FindAsync(
            j => j.UserId == userId
                && j.DeletedAt == null
                && (statusFilter == null || j.ParseStatus == statusFilter),
            includeProperties: includes,
            cancellationToken: cancellationToken))
            .OrderByDescending(j => j.CreatedAt)
            .ToList();

        var totalItems = all.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JdSubmissionListItemDto(
                j.Id,
                j.SourceType.ToString().ToLowerInvariant(),
                j.JobTitle,
                j.JobRoleCategory,
                j.SeniorityLevel,
                j.ParseStatus.ToString().ToLowerInvariant(),
                j.CreatedAt,
                HasAssessmentPath: j.AssessmentPath != null,
                HasGapAnalysis: j.GapAnalysis != null,
                HasActiveRoadmap: j.Roadmap != null && j.Roadmap.Status == RoadmapStatus.Active))
            .ToList();

        return new PagedResult<JdSubmissionListItemDto>(
            items,
            new PaginationDto(page, pageSize, totalItems, totalPages));
    }
}
