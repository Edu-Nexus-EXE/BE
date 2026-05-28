using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Enums.Roadmaps;
using MediatR;

namespace Edu_Nexus.Application.Features.Roadmaps.Commands;

public record ArchiveRoadmapCommand(Guid Id) : IRequest<ArchiveRoadmapResponseDto>;

public class ArchiveRoadmapCommandHandler : IRequestHandler<ArchiveRoadmapCommand, ArchiveRoadmapResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ArchiveRoadmapCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ArchiveRoadmapResponseDto> Handle(ArchiveRoadmapCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var roadmap = await _unitOfWork.Roadmaps.FirstOrDefaultAsync(
            r => r.Id == request.Id && r.UserId == userId,
            "", cancellationToken)
            ?? throw new Exception("404 ROADMAP_NOT_FOUND");

        roadmap.Status = RoadmapStatus.Archived;
        roadmap.IsOutdated = false;
        
        _unitOfWork.Roadmaps.Update(roadmap);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ArchiveRoadmapResponseDto(roadmap.Id, roadmap.Status.ToString().ToLowerInvariant());
    }
}

public record ArchiveRoadmapResponseDto(Guid Id, string Status);
