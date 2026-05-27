using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;

namespace Edu_Nexus.Application.Features.Roadmaps.Commands;

public record KeepRoadmapCommand(Guid Id) : IRequest<KeepRoadmapResponseDto>;

public class KeepRoadmapCommandHandler : IRequestHandler<KeepRoadmapCommand, KeepRoadmapResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public KeepRoadmapCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<KeepRoadmapResponseDto> Handle(KeepRoadmapCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var roadmap = await _unitOfWork.Roadmaps.FirstOrDefaultAsync(
            r => r.Id == request.Id && r.UserId == userId,
            "", cancellationToken)
            ?? throw new Exception("404 ROADMAP_NOT_FOUND");

        roadmap.IsOutdated = false;
        
        _unitOfWork.Roadmaps.Update(roadmap);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new KeepRoadmapResponseDto(roadmap.Id, roadmap.IsOutdated);
    }
}

public record KeepRoadmapResponseDto(Guid Id, bool IsOutdated);
