using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.CareerTracks.Commands;

public class DeleteCareerTrackCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}

public class DeleteCareerTrackCommandHandler : IRequestHandler<DeleteCareerTrackCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCareerTrackCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteCareerTrackCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new Exception("401 UNAUTHORIZED");

        // Need to delete only the Career Track and CareerTrackJds, not the JDs themselves
        var careerTrack = await _unitOfWork.CareerTracks
            .FirstOrDefaultAsync(ct => ct.Id == request.Id && ct.UserId == userId, "CareerTrackJds", cancellationToken);

        if (careerTrack == null)
            throw new Exception("404 NOT_FOUND");

        foreach(var item in careerTrack.CareerTrackJds.ToList())
        {
            _unitOfWork.CareerTrackJds.Remove(item);
        }
        _unitOfWork.CareerTracks.Remove(careerTrack);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
