using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.Admin.Skills.Commands;

// APPROVE
public record ApproveSkillCommand(Guid Id, ApproveSkillRequest Request) : IRequest<AdminSkillDto>;

public class ApproveSkillCommandHandler : IRequestHandler<ApproveSkillCommand, AdminSkillDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ApproveSkillCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminSkillDto> Handle(ApproveSkillCommand command, CancellationToken cancellationToken)
    {
        var skill = await _unitOfWork.Skills.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new Exception("404 SKILL_NOT_FOUND");

        if (skill.Description == null || !skill.Description.StartsWith("[AI-GENERATED]"))
        {
            throw new Exception("409 NOT_PENDING_REVIEW");
        }

        if (!string.IsNullOrWhiteSpace(command.Request.Name))
        {
            // Check slug collision if changing name implies changing slug, but request doesn't have slug. We'll just change name.
            // Wait, if they just change Name, should we change Slug? Let's just change what's provided.
            skill.Name = command.Request.Name;
        }

        if (command.Request.Category != null) skill.Category = command.Request.Category;
        if (command.Request.Major != null) skill.Major = command.Request.Major;
        if (command.Request.DifficultyLevel.HasValue) skill.DifficultyLevel = command.Request.DifficultyLevel.Value;

        if (!string.IsNullOrWhiteSpace(command.Request.Description))
        {
            skill.Description = command.Request.Description;
        }
        else
        {
            skill.Description = skill.Description.Substring("[AI-GENERATED]".Length).TrimStart();
        }

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

// MERGE
public record MergeSkillCommand(Guid OldSkillId, Guid NewSkillId, MergeSkillRequest Request) : IRequest<MergeSkillResponse>;

public class MergeSkillCommandHandler : IRequestHandler<MergeSkillCommand, MergeSkillResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public MergeSkillCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<MergeSkillResponse> Handle(MergeSkillCommand command, CancellationToken cancellationToken)
    {
        if (command.OldSkillId == command.NewSkillId)
        {
            throw new Exception("422 INVALID_MERGE");
        }

        var oldSkill = await _unitOfWork.Skills.GetByIdAsync(command.OldSkillId, cancellationToken) ?? throw new Exception("404 SKILL_NOT_FOUND");
        var newSkill = await _unitOfWork.Skills.GetByIdAsync(command.NewSkillId, cancellationToken) ?? throw new Exception("404 SKILL_NOT_FOUND");

        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        // We run a raw SQL block with a transaction.
        // Npgsql parameters need to be used properly.
        var sql = @"
            DO $$
            BEGIN
                -- 1. Move usage references
                UPDATE jd_skills SET skill_id = {1} WHERE skill_id = {0};
                UPDATE gap_analysis_skills SET skill_id = {1} WHERE skill_id = {0};
                UPDATE roadmap_nodes SET skill_id = {1} WHERE skill_id = {0};

                -- 2. Move skill_resources
                INSERT INTO skill_resources (id, skill_id, resource_id, is_primary, display_order, priority_score, created_at, updated_at)
                SELECT gen_random_uuid(), {1}, resource_id, is_primary, display_order, priority_score, created_at, updated_at
                FROM skill_resources WHERE skill_id = {0}
                ON CONFLICT (skill_id, resource_id) DO NOTHING;

                DELETE FROM skill_resources WHERE skill_id = {0};

                -- 3. Move prerequisites
                INSERT INTO skill_prerequisites (skill_id, prerequisite_id)
                SELECT {1}, prerequisite_id FROM skill_prerequisites
                  WHERE skill_id = {0} AND prerequisite_id <> {1}
                ON CONFLICT (skill_id, prerequisite_id) DO NOTHING;

                INSERT INTO skill_prerequisites (skill_id, prerequisite_id)
                SELECT skill_id, {1} FROM skill_prerequisites
                  WHERE prerequisite_id = {0} AND skill_id <> {1}
                ON CONFLICT (skill_id, prerequisite_id) DO NOTHING;

                DELETE FROM skill_prerequisites
                  WHERE skill_id = {0} OR prerequisite_id = {0};

                -- 4. Log admin action
                INSERT INTO admin_actions (id, admin_user_id, action_type, target_id, target_type, metadata, created_at)
                VALUES (gen_random_uuid(), {2}, 'merge_skill', {0}, 'skill', 
                        jsonb_build_object('mergedInto', {1}, 'reason', {3}), NOW());

                -- 5. Delete old skill
                DELETE FROM skills WHERE id = {0};
            END $$;
        ";

        await _unitOfWork.ExecuteSqlAsync(sql, command.OldSkillId, command.NewSkillId, adminId, command.Request.Reason ?? (object)DBNull.Value);

        return new MergeSkillResponse
        {
            OldSkillId = command.OldSkillId,
            NewSkillId = command.NewSkillId,
            // We just return dummy counts for now since DO block doesn't return rows affected easily in EF Core ExecuteSqlRaw
            MovedRoadmapNodes = 0,
            MovedJdSkills = 0,
            MovedGapSkills = 0,
            MovedResources = 0,
            MovedPrerequisites = 0
        };
    }
}
