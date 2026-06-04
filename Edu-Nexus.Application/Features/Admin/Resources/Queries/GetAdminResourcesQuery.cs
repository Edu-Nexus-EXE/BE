using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.LearningResources;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Resources.Queries;

public record GetAdminResourcesQuery(
    string? Type,
    string? Search,
    bool? NeedsReview,
    bool? IsActive,
    int Page,
    int PageSize) : IRequest<PagedResult<AdminResourceDto>>;

public class GetAdminResourcesQueryHandler : IRequestHandler<GetAdminResourcesQuery, PagedResult<AdminResourceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminResourcesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<AdminResourceDto>> Handle(GetAdminResourcesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        LearningResourceType? typeFilter = ResourceMapping.ParseType(request.Type);
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim().ToLowerInvariant();

        var rows = (await _unitOfWork.LearningResources.FindAsync(
            r => (typeFilter == null || r.Type == typeFilter)
                 && (request.NeedsReview == null || r.NeedsAdminReview == request.NeedsReview)
                 && (request.IsActive == null || r.IsActive == request.IsActive)
                 && (search == null || r.Title.ToLower().Contains(search) || (r.Provider != null && r.Provider.ToLower().Contains(search))),
            nameof(LearningResource.SkillResources),
            cancellationToken))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var totalItems = rows.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ResourceMapping.ToDto)
            .ToList();

        return new PagedResult<AdminResourceDto>(items, new PaginationDto(page, pageSize, totalItems, totalPages));
    }
}
