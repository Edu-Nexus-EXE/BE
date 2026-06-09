using System.Text.Json;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.AssessmentQuestions;
using Edu_Nexus.Domain.Enums.JdSkills;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Jobs;

public class AssessmentGenerateJob
{
    private const int DefaultPart1Count = 11;
    private const int DefaultPart2Count = 7;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAssessmentQuestionGenerator _generator;
    private readonly IRagService _rag;
    private readonly ILogger<AssessmentGenerateJob> _logger;

    public AssessmentGenerateJob(
        IUnitOfWork unitOfWork,
        IAssessmentQuestionGenerator generator,
        IRagService rag,
        ILogger<AssessmentGenerateJob> logger)
    {
        _unitOfWork = unitOfWork;
        _generator = generator;
        _rag = rag;
        _logger = logger;
    }

    public async Task RunAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.AssessmentSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, nameof(AssessmentSession.AssessmentPath), cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("AssessmentGenerateJob: session {Id} not found", sessionId);
            return;
        }

        if (session.AssessmentQuestions.Any())
        {
            _logger.LogInformation("AssessmentGenerateJob: session {Id} already has questions, skipping", sessionId);
            return;
        }

        try
        {
            var jdId = session.AssessmentPath.JdId;
            var jd = await _unitOfWork.JdSubmissions
                .FirstOrDefaultAsync(j => j.Id == jdId, nameof(JdSubmission.JdSkills), cancellationToken);

            var hardSkills = jd?.JdSkills
                .Where(s => s.SkillType == SkillType.HardSkill)
                .Select(s => s.SkillNameRaw)
                .ToList()
                ?? new List<string>();

            // Retrieve RAG context for each skill to provide knowledge base context for question generation
            var ragContextBySkill = new Dictionary<string, List<string>>();
            foreach (var skill in hardSkills)
            {
                var chunks = await _rag.SearchBySkillAsync(skill, limit: 3, cancellationToken);
                ragContextBySkill[skill] = chunks.Select(c => c.Content).ToList();
                _logger.LogDebug("AssessmentGenerateJob: retrieved {Count} RAG chunks for skill {Skill}", chunks.Count, skill);
            }

            var input = new AssessmentGenerationInput(
                JobRoleCategory: jd?.JobRoleCategory ?? "general_software",
                SeniorityLevel: jd?.SeniorityLevel,
                HardSkills: hardSkills,
                Part1Target: DefaultPart1Count,
                Part2Target: DefaultPart2Count);

            // TODO: Pass ragContextBySkill to question generator when AssessmentGenerationInput is extended
            // For now, the generator uses heuristic/fake implementation without RAG context
            var questions = await _generator.GenerateAsync(input, cancellationToken);

            short seq = 1;
            foreach (var q in questions)
            {
                var optionsJson = JsonSerializer.Serialize(new
                {
                    A = q.OptionA,
                    B = q.OptionB,
                    C = q.OptionC,
                    D = q.OptionD,
                });

                _unitOfWork.AssessmentQuestions.Add(new AssessmentQuestion
                {
                    SessionId = sessionId,
                    SequenceOrder = seq++,
                    Part = q.Part == 2 ? AssessmentQuestionPart.Part2 : AssessmentQuestionPart.Part1,
                    QuestionText = q.QuestionText,
                    Options = optionsJson,
                    CorrectOption = Enum.Parse<AssessmentOption>(q.CorrectOption, true),
                    RelatedSkill = q.RelatedSkill,
                    Explanation = q.Explanation,
                });
            }

            session.Part1Count = (short)questions.Count(q => q.Part == 1);
            session.Part2Count = (short)questions.Count(q => q.Part == 2);
            _unitOfWork.AssessmentSessions.Update(session);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("AssessmentGenerateJob completed for {Id}, generated {Count} questions", sessionId, questions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AssessmentGenerateJob failed for {Id}", sessionId);
            throw;
        }
    }
}
