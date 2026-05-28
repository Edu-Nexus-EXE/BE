using Edu_Nexus.Application.Features.CareerTracks.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Enums.Roadmaps;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.CareerTracks.Queries;

public class GetCareerTracksQuery : IRequest<List<CareerTrackDto>>
{
}

public class GetCareerTracksQueryHandler : IRequestHandler<GetCareerTracksQuery, List<CareerTrackDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetCareerTracksQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<List<CareerTrackDto>> Handle(GetCareerTracksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new Exception("401 UNAUTHORIZED");

        var careerTracks = (await _unitOfWork.CareerTracks
            .FindAsync(ct => ct.UserId == userId, "CareerTrackJds", cancellationToken)).ToList();

        var jdIds = careerTracks.SelectMany(ct => ct.CareerTrackJds).Select(ctj => ctj.JdId).Distinct().ToList();

        var activeRoadmaps = (await _unitOfWork.Roadmaps
            .FindAsync(r => jdIds.Contains(r.JdId) && r.Status == RoadmapStatus.Active, "", cancellationToken)).ToList();

        var roadmapProgressByJdId = activeRoadmaps.ToDictionary(r => r.JdId, r => r.ProgressPercent);

        var result = new List<CareerTrackDto>();

        foreach (var ct in careerTracks)
        {
            int overallProgress = 0;
            var jdCount = ct.CareerTrackJds.Count;

            if (jdCount > 0)
            {
                int totalProgress = 0;
                foreach (var link in ct.CareerTrackJds)
                {
                    if (roadmapProgressByJdId.TryGetValue(link.JdId, out var progress))
                    {
                        totalProgress += progress;
                    }
                }
                overallProgress = totalProgress / jdCount;
            }

            result.Add(new CareerTrackDto
            {
                Id = ct.Id,
                Name = ct.Name,
                Description = ct.Description,
                JdCount = jdCount,
                OverallProgress = overallProgress,
                CreatedAt = ct.CreatedAt
            });
        }

        return result;
    }
}
