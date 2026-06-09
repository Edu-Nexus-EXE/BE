using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.GapAnalysisSkills;
using Edu_Nexus.Domain.Enums.RoadmapNodes;
using Edu_Nexus.Domain.Enums.Roadmaps;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Jobs;

public class RoadmapGenerateJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRoadmapGeneratorService _generator;
    private readonly ILogger<RoadmapGenerateJob> _logger;

    public RoadmapGenerateJob(IUnitOfWork unitOfWork, IRoadmapGeneratorService generator, ILogger<RoadmapGenerateJob> logger)
    {
        _unitOfWork = unitOfWork;
        _generator = generator;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid roadmapId, CancellationToken cancellationToken)
    {
        var roadmap = await _unitOfWork.Roadmaps.FirstOrDefaultAsync(r => r.Id == roadmapId, "", cancellationToken);
        if (roadmap == null || roadmap.Status != RoadmapStatus.Generating)
        {
            _logger.LogWarning("Roadmap {Id} not found or not Generating", roadmapId);
            return;
        }

        try
        {
            var gap = (await _unitOfWork.GapAnalyses.FindAsync(
                g => g.JdId == roadmap.JdId && g.UserId == roadmap.UserId
                     && g.Status == Domain.Enums.GapAnalyses.GapAnalysisStatus.Completed,
                "", cancellationToken))
                .OrderByDescending(g => g.Version).FirstOrDefault()
                ?? throw new InvalidOperationException("No completed gap analysis for roadmap");

            var gapSkills = (await _unitOfWork.GapAnalysisSkills.FindAsync(
                s => s.GapAnalysisId == gap.Id, "", cancellationToken))
                .Select(s => new GapSkillInput(
                    s.SkillName,
                    s.GapStatus switch
                    {
                        GapStatus.Missing => "missing",
                        GapStatus.NeedsUpgrade => "needs_upgrade",
                        _ => "have"
                    },
                    s.UrgencyScore ?? 5,
                    s.IsMandatoryInJd))
                .ToList();

            var jd = await _unitOfWork.JdSubmissions.FirstOrDefaultAsync(j => j.Id == roadmap.JdId, "", cancellationToken);
            var jobTitle = jd?.JobTitle ?? "Software Developer";

            var result = await _generator.GenerateAsync(roadmap.Id, gap.Id, jobTitle, gapSkills, cancellationToken);
            if (!result.Success)
            {
                roadmap.Status = RoadmapStatus.Failed;
                _unitOfWork.Roadmaps.Update(roadmap);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogWarning("Roadmap {Id} generation failed: {Error}", roadmapId, result.ErrorMessage);
                return;
            }

            var bySequence = new Dictionary<int, RoadmapNode>();
            foreach (var n in result.Nodes)
            {
                var node = new RoadmapNode
                {
                    RoadmapId = roadmap.Id,
                    SkillId = n.SkillId,
                    SkillName = n.SkillName,
                    Description = n.Description,
                    SequenceOrder = n.SequenceOrder,
                    EstimatedHours = n.EstimatedHours,
                    IsPrerequisite = n.PrerequisiteSequences.Count == 0,
                    Status = RoadmapNodeStatus.NotStarted,
                };
                _unitOfWork.RoadmapNodes.Add(node);
                bySequence[n.SequenceOrder] = node;
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var n in result.Nodes)
            {
                if (!bySequence.TryGetValue(n.SequenceOrder, out var node)) continue;
                foreach (var prereqSeq in n.PrerequisiteSequences)
                {
                    if (bySequence.TryGetValue(prereqSeq, out var prereq) && prereq.Id != node.Id)
                        node.PrerequisiteNodes.Add(prereq);
                }
                _unitOfWork.RoadmapNodes.Update(node);
            }

            roadmap.Title = result.Title ?? jobTitle;
            roadmap.EstimatedTotalHours = result.EstimatedTotalHours;
            roadmap.GapAnalysisId = gap.Id;
            roadmap.Status = RoadmapStatus.Active;
            _unitOfWork.Roadmaps.Update(roadmap);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Roadmap {Id} generated with {Count} nodes", roadmapId, result.Nodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RoadmapGenerateJob failed for {Id}", roadmapId);
            roadmap.Status = RoadmapStatus.Failed;
            _unitOfWork.Roadmaps.Update(roadmap);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
