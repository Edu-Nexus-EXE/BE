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

public class GetCareerTrackByIdQuery : IRequest<CareerTrackDetailDto>
{
    public Guid Id { get; set; }
}

public class GetCareerTrackByIdQueryHandler : IRequestHandler<GetCareerTrackByIdQuery, CareerTrackDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetCareerTrackByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CareerTrackDetailDto> Handle(GetCareerTrackByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new Exception("401 UNAUTHORIZED");

        var careerTrack = await _unitOfWork.CareerTracks
            .FirstOrDefaultAsync(ct => ct.Id == request.Id && ct.UserId == userId, "CareerTrackJds.Jd", cancellationToken);

        if (careerTrack == null)
            throw new Exception("404 NOT_FOUND");

        var jdIds = careerTrack.CareerTrackJds.Select(ctj => ctj.JdId).ToList();

        var roadmaps = (await _unitOfWork.Roadmaps
            .FindAsync(r => jdIds.Contains(r.JdId), "", cancellationToken)).ToList();

        // Group roadmaps by JdId to get the active one, or the latest if none is active
        var roadmapDict = roadmaps
            .GroupBy(r => r.JdId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.Status == RoadmapStatus.Active).ThenByDescending(r => r.CreatedAt).FirstOrDefault()
            );

        var detailDto = new CareerTrackDetailDto
        {
            Id = careerTrack.Id,
            Name = careerTrack.Name,
            Description = careerTrack.Description,
            CreatedAt = careerTrack.CreatedAt,
            Jds = careerTrack.CareerTrackJds.Select(ctj =>
            {
                roadmapDict.TryGetValue(ctj.JdId, out var rm);

                return new CareerTrackJdDto
                {
                    JdId = ctj.JdId,
                    JobTitle = ctj.Jd?.JobTitle ?? "Unknown JD",
                    RoadmapStatus = rm?.Status.ToString() ?? "None",
                    RoadmapProgress = rm?.ProgressPercent ?? 0,
                    AddedAt = ctj.AddedAt
                };
            }).ToList()
        };

        return detailDto;
    }
}
