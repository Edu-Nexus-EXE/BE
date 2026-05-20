using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.Onboarding.Commands;

public record UpdateOnboardingCommand(SubmitOnboardingRequest Request) : IRequest<OnboardingResponsesDto>;

public class UpdateOnboardingCommandHandler : IRequestHandler<UpdateOnboardingCommand, OnboardingResponsesDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateOnboardingCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<OnboardingResponsesDto> Handle(UpdateOnboardingCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null)
        {
            throw new Exception("401 UNAUTHORIZED");
        }

        var existing = await _unitOfWork.OnboardingResponses
            .FirstOrDefaultAsync(o => o.UserId == currentUserId.Value, "", cancellationToken);

        if (existing == null)
        {
            throw new Exception("404 NOT_FOUND");
        }

        existing.AcademicYear = request.Request.AcademicYear;
        existing.Major = request.Request.Major;
        existing.PrimaryGoal = request.Request.PrimaryGoal;
        existing.WeeklyStudyHours = request.Request.WeeklyStudyHours;
        existing.ProficiencyLevel = request.Request.ProficiencyLevel;
        existing.LearningPriority = request.Request.LearningPriority;
        existing.LearningBudget = request.Request.LearningBudget;
        existing.PreferredChannel = request.Request.PreferredChannel;

        _unitOfWork.OnboardingResponses.Update(existing);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")
        {
            throw new Exception("422 INVALID_DATA");
        }

        return new OnboardingResponsesDto(
            existing.AcademicYear,
            existing.Major,
            existing.PrimaryGoal,
            existing.WeeklyStudyHours,
            existing.ProficiencyLevel,
            existing.LearningPriority,
            existing.LearningBudget,
            existing.PreferredChannel
        );
    }
}
