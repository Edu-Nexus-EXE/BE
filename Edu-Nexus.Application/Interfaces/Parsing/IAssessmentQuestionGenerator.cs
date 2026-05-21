namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface IAssessmentQuestionGenerator
{
    Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        AssessmentGenerationInput input,
        CancellationToken cancellationToken = default);
}

public record AssessmentGenerationInput(
    string JobRoleCategory,
    string? SeniorityLevel,
    IReadOnlyList<string> HardSkills,
    int Part1Target,
    int Part2Target);

public record GeneratedQuestion(
    int Part,
    string RelatedSkill,
    string QuestionText,
    string OptionA,
    string OptionB,
    string OptionC,
    string OptionD,
    string CorrectOption,
    string Explanation);
