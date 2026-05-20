using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;

namespace Edu_Nexus.Application.Features.Onboarding.Queries;

public record GetOnboardingQuery() : IRequest<OnboardingResponseData>;

public class GetOnboardingQueryHandler : IRequestHandler<GetOnboardingQuery, OnboardingResponseData>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetOnboardingQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<OnboardingResponseData> Handle(GetOnboardingQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null)
        {
            throw new Exception("401 UNAUTHORIZED");
        }

        var onboardingResponse = await _unitOfWork.OnboardingResponses
            .FirstOrDefaultAsync(o => o.UserId == currentUserId.Value, "", cancellationToken);

        if (onboardingResponse == null)
        {
            return new OnboardingResponseData(false, null, null);
        }

        var responsesDto = new OnboardingResponsesDto(
            onboardingResponse.AcademicYear,
            onboardingResponse.Major,
            onboardingResponse.PrimaryGoal,
            onboardingResponse.WeeklyStudyHours,
            onboardingResponse.ProficiencyLevel,
            onboardingResponse.LearningPriority,
            onboardingResponse.LearningBudget,
            onboardingResponse.PreferredChannel
        );

        return new OnboardingResponseData(true, responsesDto, onboardingResponse.UpdatedAt);
    }
}
