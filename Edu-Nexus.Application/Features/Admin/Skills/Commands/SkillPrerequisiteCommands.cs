using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.Admin.Skills.Commands;

// ADD PREREQUISITE
public record AddSkillPrerequisiteCommand(Guid Id, AddPrerequisiteRequest Request) : IRequest;

public class AddSkillPrerequisiteCommandHandler : IRequestHandler<AddSkillPrerequisiteCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddSkillPrerequisiteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddSkillPrerequisiteCommand command, CancellationToken cancellationToken)
    {
        var skill = await _unitOfWork.Skills.FirstOrDefaultAsync(s => s.Id == command.Id, "Prerequisites", cancellationToken)
            ?? throw new Exception("404 SKILL_NOT_FOUND");

        var prerequisite = await _unitOfWork.Skills.GetByIdAsync(command.Request.PrerequisiteSkillId, cancellationToken)
            ?? throw new Exception("404 PREREQUISITE_NOT_FOUND");

        // Simple cycle detection (1-level check, full DFS would require loading the graph)
        if (skill.Id == prerequisite.Id || prerequisite.Prerequisites.Any(p => p.Id == skill.Id))
        {
            throw new Exception("422 CYCLE_DETECTED");
        }

        if (!skill.Prerequisites.Any(p => p.Id == prerequisite.Id))
        {
            skill.Prerequisites.Add(prerequisite);
            _unitOfWork.Skills.Update(skill);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

// REMOVE PREREQUISITE
public record RemoveSkillPrerequisiteCommand(Guid Id, Guid PrereqId) : IRequest;

public class RemoveSkillPrerequisiteCommandHandler : IRequestHandler<RemoveSkillPrerequisiteCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveSkillPrerequisiteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveSkillPrerequisiteCommand command, CancellationToken cancellationToken)
    {
        var skill = await _unitOfWork.Skills.FirstOrDefaultAsync(s => s.Id == command.Id, "Prerequisites", cancellationToken)
            ?? throw new Exception("404 SKILL_NOT_FOUND");

        var prereq = skill.Prerequisites.FirstOrDefault(p => p.Id == command.PrereqId);
        if (prereq != null)
        {
            skill.Prerequisites.Remove(prereq);
            _unitOfWork.Skills.Update(skill);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
