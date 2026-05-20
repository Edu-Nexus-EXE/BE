using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using MediatR;

namespace Edu_Nexus.Application.Features.Onboarding.Commands;

public record SubmitOnboardingCommand(SubmitOnboardingRequest Request) : IRequest<OnboardingResponsesDto>;

public class SubmitOnboardingCommandHandler : IRequestHandler<SubmitOnboardingCommand, OnboardingResponsesDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SubmitOnboardingCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<OnboardingResponsesDto> Handle(SubmitOnboardingCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null)
        {
            throw new Exception("401 UNAUTHORIZED");
        }

        var existing = await _unitOfWork.OnboardingResponses
            .FirstOrDefaultAsync(o => o.UserId == currentUserId.Value, "", cancellationToken);

        if (existing != null)
        {
            throw new Exception("409 ALREADY_COMPLETED");
        }

        var newResponse = new OnboardingResponse
        {
            UserId = currentUserId.Value,
            AcademicYear = request.Request.AcademicYear,
            Major = request.Request.Major,
            PrimaryGoal = request.Request.PrimaryGoal,
            WeeklyStudyHours = request.Request.WeeklyStudyHours,
            ProficiencyLevel = request.Request.ProficiencyLevel,
            LearningPriority = request.Request.LearningPriority,
            LearningBudget = request.Request.LearningBudget,
            PreferredChannel = request.Request.PreferredChannel
        };

        _unitOfWork.OnboardingResponses.Add(newResponse);

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == currentUserId.Value && u.DeletedAt == null, "", cancellationToken);
        if (user != null)
        {
            user.IsSurveyCompleted = true;
            _unitOfWork.Users.Update(user);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")
        {
            throw new Exception("422 INVALID_DATA");
        }

        return new OnboardingResponsesDto(
            newResponse.AcademicYear,
            newResponse.Major,
            newResponse.PrimaryGoal,
            newResponse.WeeklyStudyHours,
            newResponse.ProficiencyLevel,
            newResponse.LearningPriority,
            newResponse.LearningBudget,
            newResponse.PreferredChannel
        );
    }
}
