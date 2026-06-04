using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using MediatR;
using System.Linq;

namespace Edu_Nexus.Application.Features.Admin.Skills.Queries;

// GET /admin/skills
public record GetSkillsQuery(string? Search, string? Major, string? Category, bool? IsActive, int Page = 1, int PageSize = 20) : IRequest<PagedResult<AdminSkillDto>>;

public class GetSkillsQueryHandler : IRequestHandler<GetSkillsQuery, PagedResult<AdminSkillDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetSkillsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<AdminSkillDto>> Handle(GetSkillsQuery request, CancellationToken cancellationToken)
    {
        // Wait, IRepository<Skill>.GetAllAsync doesn't support IQueryable. It returns IEnumerable.
        // Let's use FindAsync.
        var skills = await _unitOfWork.Skills.FindAsync(s => 
            (string.IsNullOrEmpty(request.Search) || s.Name.Contains(request.Search)) &&
            (string.IsNullOrEmpty(request.Major) || s.Major == request.Major) &&
            (string.IsNullOrEmpty(request.Category) || s.Category == request.Category) &&
            (!request.IsActive.HasValue || s.IsActive == request.IsActive.Value), 
            "", cancellationToken);

        var total = skills.Count();
        var pagedSkills = skills
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new AdminSkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Slug = s.Slug,
                Category = s.Category,
                Major = s.Major,
                Description = s.Description,
                DifficultyLevel = s.DifficultyLevel,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToList();

        return new PagedResult<AdminSkillDto>
        {
            Data = pagedSkills,
            Pagination = new PaginationMetadata
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)request.PageSize)
            }
        };
    }
}

public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();
}

public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
