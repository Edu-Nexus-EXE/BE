using System.Text.Json;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Application.Interfaces.Storage;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.JdSubmissions;
using Edu_Nexus.Domain.Enums.Roadmaps;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Jobs;

public class CvParseJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly IAnonymizer _anonymizer;
    private readonly ICvParser _cvParser;
    private readonly ILogger<CvParseJob> _logger;

    public CvParseJob(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IPdfTextExtractor pdfExtractor,
        IAnonymizer anonymizer,
        ICvParser cvParser,
        ILogger<CvParseJob> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _pdfExtractor = pdfExtractor;
        _anonymizer = anonymizer;
        _cvParser = cvParser;
        _logger = logger;
    }

    public async Task RunAsync(Guid cvSubmissionId, bool isReupload, CancellationToken cancellationToken)
    {
        var cv = await _unitOfWork.CvSubmissions
            .FirstOrDefaultAsync(c => c.Id == cvSubmissionId, "", cancellationToken);

        if (cv == null)
        {
            _logger.LogWarning("CvParseJob: submission {Id} not found", cvSubmissionId);
            return;
        }

        try
        {
            cv.ParseStatus = ParseStatus.Processing;
            _unitOfWork.CvSubmissions.Update(cv);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            string rawText;
            await using (var stream = await _fileStorage.OpenReadAsync(cv.FileUrl, cancellationToken))
            {
                rawText = _pdfExtractor.Extract(stream);
            }

            var masked = _anonymizer.Mask(rawText);
            var parsed = await _cvParser.ParseAsync(masked, cancellationToken);

            cv.ParsedText = masked;
            cv.ParsedSkills = JsonSerializer.Serialize(new
            {
                totalExperienceYears = parsed.TotalExperienceYears,
                skills = parsed.Skills.Select(s => new
                {
                    skillName = s.SkillName,
                    proficiencyLevel = s.ProficiencyLevel,
                    yearsExp = s.YearsExp,
                    evidence = s.Evidence
                })
            });
            cv.ParseStatus = ParseStatus.Completed;
            cv.ParsedAt = DateTime.UtcNow;
            cv.ParseError = null;
            _unitOfWork.CvSubmissions.Update(cv);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (isReupload)
            {
                await MarkRoadmapsOutdatedAsync(cv.AssessmentPathId, cancellationToken);
            }

            _logger.LogInformation("CvParseJob completed for {Id} (reupload={IsReupload})", cvSubmissionId, isReupload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CvParseJob failed for {Id}", cvSubmissionId);
            cv.ParseStatus = ParseStatus.Failed;
            cv.ParseError = ex.Message;
            _unitOfWork.CvSubmissions.Update(cv);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task MarkRoadmapsOutdatedAsync(Guid assessmentPathId, CancellationToken ct)
    {
        var path = await _unitOfWork.AssessmentPaths
            .FirstOrDefaultAsync(p => p.Id == assessmentPathId, "", ct);
        if (path == null) return;

        var active = await _unitOfWork.Roadmaps.FindAsync(
            r => r.JdId == path.JdId && r.Status == RoadmapStatus.Active,
            "", ct);

        foreach (var roadmap in active)
        {
            roadmap.IsOutdated = true;
            _unitOfWork.Roadmaps.Update(roadmap);
        }

        if (active.Any())
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
