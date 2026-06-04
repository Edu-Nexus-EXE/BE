using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Edu_Nexus.Application.Features.Admin.Skills.Queries;

public record GetPendingReviewSkillsQuery(string? Search, string? Major, int Page = 1, int PageSize = 20) : IRequest<PagedResult<PendingReviewSkillDto>>;

public class GetPendingReviewSkillsQueryHandler : IRequestHandler<GetPendingReviewSkillsQuery, PagedResult<PendingReviewSkillDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetPendingReviewSkillsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<PendingReviewSkillDto>> Handle(GetPendingReviewSkillsQuery request, CancellationToken cancellationToken)
    {
        var skills = await _unitOfWork.Skills.FindAsync(s => 
            s.Description != null && s.Description.StartsWith("[AI-GENERATED]") &&
            s.IsActive == true &&
            (string.IsNullOrEmpty(request.Search) || s.Name.Contains(request.Search)) &&
            (string.IsNullOrEmpty(request.Major) || s.Major == request.Major), 
            "", cancellationToken);

        var total = skills.Count();
        var pagedSkills = skills
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = new List<PendingReviewSkillDto>();

        foreach (var s in pagedSkills)
        {
            var roadmapNodesCount = (await _unitOfWork.RoadmapNodes.FindAsync(n => n.SkillId == s.Id, "", cancellationToken)).Count();
            var jdSkillsCount = (await _unitOfWork.JdSkills.FindAsync(j => j.SkillId == s.Id, "", cancellationToken)).Count();
            var resourcesCount = (await _unitOfWork.SkillResources.FindAsync(r => r.SkillId == s.Id, "", cancellationToken)).Count();

            dtos.Add(new PendingReviewSkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Slug = s.Slug,
                Category = s.Category,
                Major = s.Major,
                Description = s.Description,
                DifficultyLevel = s.DifficultyLevel,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                RoadmapUsage = roadmapNodesCount,
                JdUsage = jdSkillsCount,
                ResourceCount = resourcesCount
            });
        }

        return new PagedResult<PendingReviewSkillDto>
        {
            Data = dtos,
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
