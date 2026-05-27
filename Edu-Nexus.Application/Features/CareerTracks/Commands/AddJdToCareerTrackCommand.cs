using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.CareerTracks.Commands;

public class AddJdToCareerTrackCommand : IRequest<Unit>
{
    public Guid CareerTrackId { get; set; }
    public Guid JdId { get; set; }
}

public class AddJdToCareerTrackCommandHandler : IRequestHandler<AddJdToCareerTrackCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddJdToCareerTrackCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(AddJdToCareerTrackCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        // Check if Career Track exists and belongs to user
        var careerTrackExists = (await _unitOfWork.CareerTracks
            .FindAsync(ct => ct.Id == request.CareerTrackId && ct.UserId == userId, "", cancellationToken)).Any();
            
        if (!careerTrackExists)
            throw new KeyNotFoundException($"CareerTrack with id {request.CareerTrackId} not found.");

        // Check if JD exists and belongs to user
        var jdExists = (await _unitOfWork.JdSubmissions
            .FindAsync(jd => jd.Id == request.JdId && jd.UserId == userId, "", cancellationToken)).Any();
            
        if (!jdExists)
            throw new KeyNotFoundException($"JdSubmission with id {request.JdId} not found.");

        // Check if already exists
        var existingLink = (await _unitOfWork.CareerTrackJds
            .FindAsync(ctj => ctj.CareerTrackId == request.CareerTrackId && ctj.JdId == request.JdId, "", cancellationToken)).Any();
            
        if (existingLink)
            throw new InvalidOperationException("JD is already in the career track.");

        var newLink = new CareerTrackJd
        {
            CareerTrackId = request.CareerTrackId,
            JdId = request.JdId,
            AddedAt = DateTime.UtcNow
        };

        _unitOfWork.CareerTrackJds.Add(newLink);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
