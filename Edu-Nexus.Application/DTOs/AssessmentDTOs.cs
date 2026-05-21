namespace Edu_Nexus.Application.DTOs;

public record StartAssessmentSessionRequest(Guid? ReuseSessionId);

public record SessionAcceptedDto(
    Guid SessionId,
    string Status,
    Guid? ReusedFromSessionId,
    DateTime StartedAt);

public record QuestionOptionsDto(string A, string B, string C, string D);

public record AssessmentQuestionDto(
    Guid Id,
    short SequenceOrder,
    int Part,
    string QuestionText,
    QuestionOptionsDto Options);

public record SessionQuestionsDto(
    Guid SessionId,
    string Status,
    short Part1Count,
    short Part2Count,
    IReadOnlyList<AssessmentQuestionDto> Questions);

public record SubmitAnswerDto(Guid QuestionId, string SelectedOption);
public record SubmitAssessmentSessionRequest(IReadOnlyList<SubmitAnswerDto> Answers);

public record SkillScoreDto(
    string SkillName,
    int Score,
    int MaxScore,
    string ProficiencyLevel);

public record AnswerResultDto(
    Guid QuestionId,
    string SelectedOption,
    string CorrectOption,
    bool IsCorrect,
    string? Explanation);

public record AutoTriggeredDto(Guid GapAnalysisId, string GapAnalysisStatus);

public record AssessmentSessionResultDto(
    Guid SessionId,
    string Status,
    int TotalQuestions,
    int CorrectCount,
    decimal ScorePercent,
    IReadOnlyList<SkillScoreDto> SkillScores,
    IReadOnlyList<AnswerResultDto> Results,
    DateTime? SubmittedAt,
    AutoTriggeredDto? AutoTriggered);

public record ReusableSessionDto(
    Guid SessionId,
    string JobRoleCategorySnapshot,
    decimal ScorePercent,
    DateTime? SubmittedAt,
    string? FromJdTitle);
