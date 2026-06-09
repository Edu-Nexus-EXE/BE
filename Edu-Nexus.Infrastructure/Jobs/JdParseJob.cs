using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.JdSkills;
using Edu_Nexus.Domain.Enums.JdSubmissions;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Jobs;

public class JdParseJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJdParser _jdParser;
    private readonly IAnonymizer _anonymizer;
    private readonly IJdUrlFetcherService _jdUrlFetcher;
    private readonly ISkillMatcherBatchService _skillMatcher;
    private readonly ILogger<JdParseJob> _logger;

    public JdParseJob(IUnitOfWork unitOfWork, IJdParser jdParser, IAnonymizer anonymizer, IJdUrlFetcherService jdUrlFetcher, ISkillMatcherBatchService skillMatcher, ILogger<JdParseJob> logger)
    {
        _unitOfWork = unitOfWork;
        _jdParser = jdParser;
        _anonymizer = anonymizer;
        _jdUrlFetcher = jdUrlFetcher;
        _skillMatcher = skillMatcher;
        _logger = logger;
    }

    public async Task RunAsync(Guid jdSubmissionId, CancellationToken cancellationToken)
    {
        var jd = await _unitOfWork.JdSubmissions
            .FirstOrDefaultAsync(j => j.Id == jdSubmissionId, "", cancellationToken);

        if (jd == null)
        {
            _logger.LogWarning("JdParseJob: submission {Id} not found", jdSubmissionId);
            return;
        }

        try
        {
            jd.ParseStatus = ParseStatus.Processing;
            _unitOfWork.JdSubmissions.Update(jd);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var raw = jd.RawContent ?? string.Empty;
            if (jd.SourceType == JdSourceType.Url && !string.IsNullOrWhiteSpace(jd.SourceUrl))
            {
                raw = await _jdUrlFetcher.FetchAsync(jd.SourceUrl, cancellationToken);
                jd.RawContent = raw; // Save it back so we have the raw text
            }
            var masked = _anonymizer.Mask(raw);
            var parsed = await _jdParser.ParseAsync(masked, cancellationToken);

            jd.JobTitle = parsed.JobTitle;
            jd.JobRoleCategory = parsed.JobRoleCategory;
            jd.SeniorityLevel = parsed.SeniorityLevel;
            jd.SalaryMin = parsed.SalaryMin;
            jd.SalaryMax = parsed.SalaryMax;
            jd.Currency = parsed.Currency;
            jd.ParseStatus = ParseStatus.Completed;
            jd.ParsedAt = DateTime.UtcNow;
            jd.ParseError = null;

            // Match all skill names to Skill IDs (both hard and soft)
            var allSkillNames = parsed.HardSkills.Select(s => s.SkillNameRaw)
                .Concat(parsed.SoftSkills.Select(s => s.SkillNameRaw))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            var skillIdMap = await _skillMatcher.MatchSkillsAsync(allSkillNames, cancellationToken);

            foreach (var s in parsed.HardSkills)
            {
                var skillId = skillIdMap.TryGetValue(s.SkillNameRaw, out var id) ? id : null;
                _unitOfWork.JdSkills.Add(new JdSkill
                {
                    JdId = jd.Id,
                    SkillNameRaw = s.SkillNameRaw,
                    SkillId = skillId,
                    SkillType = SkillType.HardSkill,
                    IsMandatory = s.IsMandatory,
                });
            }

            foreach (var s in parsed.SoftSkills)
            {
                var skillId = skillIdMap.TryGetValue(s.SkillNameRaw, out var id) ? id : null;
                _unitOfWork.JdSkills.Add(new JdSkill
                {
                    JdId = jd.Id,
                    SkillNameRaw = s.SkillNameRaw,
                    SkillId = skillId,
                    SkillType = SkillType.SoftSkill,
                    IsMandatory = s.IsMandatory,
                });
            }

            _unitOfWork.JdSubmissions.Update(jd);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("JdParseJob completed for {Id}", jdSubmissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JdParseJob failed for {Id}", jdSubmissionId);
            jd.ParseStatus = ParseStatus.Failed;
            jd.ParseError = ex.Message;
            _unitOfWork.JdSubmissions.Update(jd);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
