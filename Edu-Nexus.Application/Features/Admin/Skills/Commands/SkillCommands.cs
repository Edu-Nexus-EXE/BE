using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.Admin.Skills.Commands;

// CREATE
public record CreateSkillCommand(CreateSkillRequest Request) : IRequest<AdminSkillDto>;

public class CreateSkillCommandHandler : IRequestHandler<CreateSkillCommand, AdminSkillDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateSkillCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminSkillDto> Handle(CreateSkillCommand command, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.Skills.FirstOrDefaultAsync(s => s.Slug == command.Request.Slug, "", cancellationToken);
        if (existing != null) throw new Exception("409 SLUG_TAKEN");

        var skill = new Skill
        {
            Name = command.Request.Name,
            Slug = command.Request.Slug,
            Category = command.Request.Category,
            Major = command.Request.Major,
            Description = command.Request.Description,
            DifficultyLevel = command.Request.DifficultyLevel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWork.Skills.Add(skill);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminSkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Slug = skill.Slug,
            Category = skill.Category,
            Major = skill.Major,
            Description = skill.Description,
            DifficultyLevel = skill.DifficultyLevel,
            IsActive = skill.IsActive,
            CreatedAt = skill.CreatedAt
        };
    }
}

// UPDATE
public record UpdateSkillCommand(Guid Id, UpdateSkillRequest Request) : IRequest<AdminSkillDto>;

public class UpdateSkillCommandHandler : IRequestHandler<UpdateSkillCommand, AdminSkillDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSkillCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminSkillDto> Handle(UpdateSkillCommand command, CancellationToken cancellationToken)
    {
        var skill = await _unitOfWork.Skills.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new Exception("404 SKILL_NOT_FOUND");

        if (skill.Slug != command.Request.Slug)
        {
            var existing = await _unitOfWork.Skills.FirstOrDefaultAsync(s => s.Slug == command.Request.Slug, "", cancellationToken);
            if (existing != null) throw new Exception("409 SLUG_TAKEN");
        }

        skill.Name = command.Request.Name;
        skill.Slug = command.Request.Slug;
        skill.Category = command.Request.Category;
        skill.Major = command.Request.Major;
        skill.Description = command.Request.Description;
        skill.DifficultyLevel = command.Request.DifficultyLevel;

        _unitOfWork.Skills.Update(skill);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminSkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Slug = skill.Slug,
            Category = skill.Category,
            Major = skill.Major,
            Description = skill.Description,
            DifficultyLevel = skill.DifficultyLevel,
            IsActive = skill.IsActive,
            CreatedAt = skill.CreatedAt
        };
    }
}

// TOGGLE ACTIVE
public record ToggleSkillActiveCommand(Guid Id, bool IsActive) : IRequest<AdminSkillDto>;

public class ToggleSkillActiveCommandHandler : IRequestHandler<ToggleSkillActiveCommand, AdminSkillDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ToggleSkillActiveCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminSkillDto> Handle(ToggleSkillActiveCommand command, CancellationToken cancellationToken)
    {
        var skill = await _unitOfWork.Skills.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new Exception("404 SKILL_NOT_FOUND");

        skill.IsActive = command.IsActive;

        _unitOfWork.Skills.Update(skill);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminSkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Slug = skill.Slug,
            Category = skill.Category,
            Major = skill.Major,
            Description = skill.Description,
            DifficultyLevel = skill.DifficultyLevel,
            IsActive = skill.IsActive,
            CreatedAt = skill.CreatedAt
        };
    }
}
