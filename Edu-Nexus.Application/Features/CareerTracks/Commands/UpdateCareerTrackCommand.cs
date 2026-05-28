using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.CareerTracks.Commands;

public class UpdateCareerTrackCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateCareerTrackCommandHandler : IRequestHandler<UpdateCareerTrackCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCareerTrackCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(UpdateCareerTrackCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new Exception("401 UNAUTHORIZED");

        var careerTrack = await _unitOfWork.CareerTracks
            .FirstOrDefaultAsync(ct => ct.Id == request.Id && ct.UserId == userId, "", cancellationToken);

        if (careerTrack == null)
            throw new Exception("404 NOT_FOUND");

        if (!string.IsNullOrEmpty(request.Name))
        {
            careerTrack.Name = request.Name;
        }
        
        if (request.Description != null)
        {
            careerTrack.Description = request.Description;
        }

        careerTrack.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.CareerTracks.Update(careerTrack);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
