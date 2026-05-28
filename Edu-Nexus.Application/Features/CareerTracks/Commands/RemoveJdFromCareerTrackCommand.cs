using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.CareerTracks.Commands;

public class RemoveJdFromCareerTrackCommand : IRequest<Unit>
{
    public Guid CareerTrackId { get; set; }
    public Guid JdId { get; set; }
}

public class RemoveJdFromCareerTrackCommandHandler : IRequestHandler<RemoveJdFromCareerTrackCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveJdFromCareerTrackCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(RemoveJdFromCareerTrackCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new Exception("401 UNAUTHORIZED");

        // Check if Career Track exists and belongs to user
        var careerTrackExists = (await _unitOfWork.CareerTracks
            .FindAsync(ct => ct.Id == request.CareerTrackId && ct.UserId == userId, "", cancellationToken)).Any();
            
        if (!careerTrackExists)
            throw new Exception("404 NOT_FOUND");

        var existingLink = await _unitOfWork.CareerTrackJds
            .FirstOrDefaultAsync(ctj => ctj.CareerTrackId == request.CareerTrackId && ctj.JdId == request.JdId, "", cancellationToken);
            
        if (existingLink == null)
            throw new Exception("404 NOT_FOUND");

        _unitOfWork.CareerTrackJds.Remove(existingLink);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
