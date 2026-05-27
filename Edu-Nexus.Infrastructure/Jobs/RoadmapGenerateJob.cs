using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.RoadmapNodes;
using Edu_Nexus.Domain.Enums.Roadmaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Jobs;

public class RoadmapGenerateJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoadmapGenerateJob> _logger;

    public RoadmapGenerateJob(IUnitOfWork unitOfWork, ILogger<RoadmapGenerateJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid roadmapId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting FAKE Roadmap Generation for Roadmap {Id}", roadmapId);

        var roadmap = await _unitOfWork.Roadmaps.FirstOrDefaultAsync(
            r => r.Id == roadmapId,
            "", cancellationToken);

        if (roadmap == null || roadmap.Status != RoadmapStatus.Generating)
        {
            _logger.LogWarning("Roadmap {Id} not found or not in Generating status", roadmapId);
            return;
        }

        try
        {
            // Simulate AI delay
            await Task.Delay(3000, cancellationToken);

            roadmap.Title = "Roadmap Backend (Fake AI Generated)";
            
            // Create some fake nodes
            var node1 = new RoadmapNode
            {
                Id = Guid.NewGuid(),
                RoadmapId = roadmap.Id,
                SkillName = "C# Basics",
                Description = "Learn the basics of C# programming language.",
                SequenceOrder = 1,
                EstimatedHours = 10,
                IsPrerequisite = true,
                Status = RoadmapNodeStatus.NotStarted
            };

            var node2 = new RoadmapNode
            {
                Id = Guid.NewGuid(),
                RoadmapId = roadmap.Id,
                SkillName = "ASP.NET Core",
                Description = "Learn how to build web APIs with ASP.NET Core.",
                SequenceOrder = 2,
                EstimatedHours = 20,
                IsPrerequisite = true,
                Status = RoadmapNodeStatus.NotStarted
            };

            // Link prerequisites
            node2.PrerequisiteNodes.Add(node1);

            _unitOfWork.RoadmapNodes.Add(node1);
            _unitOfWork.RoadmapNodes.Add(node2);

            roadmap.Status = RoadmapStatus.Active;
            _unitOfWork.Roadmaps.Update(roadmap);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully completed FAKE Roadmap Generation for Roadmap {Id}", roadmapId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate roadmap for Roadmap {Id}", roadmapId);
            roadmap.Status = RoadmapStatus.Failed;
            _unitOfWork.Roadmaps.Update(roadmap);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
