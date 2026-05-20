namespace Edu_Nexus.Application.DTOs;

public record OnboardingResponseData(
    bool Completed,
    OnboardingResponsesDto? Responses,
    DateTime? UpdatedAt
);

public record OnboardingResponsesDto(
    string AcademicYear,
    string Major,
    string PrimaryGoal,
    string WeeklyStudyHours,
    string ProficiencyLevel,
    string LearningPriority,
    string LearningBudget,
    string PreferredChannel
);

public record SubmitOnboardingRequest(
    string AcademicYear,
    string Major,
    string PrimaryGoal,
    string WeeklyStudyHours,
    string ProficiencyLevel,
    string LearningPriority,
    string LearningBudget,
    string PreferredChannel
);
