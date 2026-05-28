using Edu_Nexus.Application.Features.CareerTracks.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.CareerTracks.Commands;

public class CreateCareerTrackCommand : IRequest<CareerTrackDto>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateCareerTrackCommandHandler : IRequestHandler<CreateCareerTrackCommand, CareerTrackDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateCareerTrackCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CareerTrackDto> Handle(CreateCareerTrackCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new Exception("401 UNAUTHORIZED");

        // Quota check
        var userSub = await _unitOfWork.UserSubscriptions
            .FirstOrDefaultAsync(us => us.UserId == userId && us.Status == UserSubscriptionStatus.Active, "Tier", cancellationToken);

        var quota = userSub?.Tier?.CareerTrackQuota ?? 1; // Default Free = 1

        var currentCount = (await _unitOfWork.CareerTracks
            .FindAsync(ct => ct.UserId == userId, "", cancellationToken)).Count();

        if (currentCount >= quota)
        {
            throw new Exception($"403 QUOTA_EXCEEDED|careerTrack|{currentCount}|{quota}");
        }

        var careerTrack = new CareerTrack
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _unitOfWork.CareerTracks.Add(careerTrack);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CareerTrackDto
        {
            Id = careerTrack.Id,
            Name = careerTrack.Name,
            Description = careerTrack.Description,
            JdCount = 0,
            OverallProgress = 0,
            CreatedAt = careerTrack.CreatedAt
        };
    }
}
