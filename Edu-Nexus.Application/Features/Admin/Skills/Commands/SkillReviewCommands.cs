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

        var oldId = command.OldSkillId;
        var newId = command.NewSkillId;
        var reason = command.Request?.Reason;

        var oldSkill = await _unitOfWork.Skills.GetByIdAsync(oldId, cancellationToken) ?? throw new Exception("404 SKILL_NOT_FOUND");
        var newSkill = await _unitOfWork.Skills.GetByIdAsync(newId, cancellationToken) ?? throw new Exception("404 SKILL_NOT_FOUND");

        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // 1. Move usage references — every value below is parameterized via FormattableString.
            int movedJdSkills = await _unitOfWork.ExecuteSqlAsync(
                $"UPDATE jd_skills SET skill_id = {newId} WHERE skill_id = {oldId}", ct);

            int movedGapSkills = await _unitOfWork.ExecuteSqlAsync(
                $"UPDATE gap_analysis_skills SET skill_id = {newId} WHERE skill_id = {oldId}", ct);

            int movedRoadmapNodes = await _unitOfWork.ExecuteSqlAsync(
                $"UPDATE roadmap_nodes SET skill_id = {newId} WHERE skill_id = {oldId}", ct);

            // 2. Move skill_resources, skipping rows that already exist on the target skill.
            int movedResources = await _unitOfWork.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO skill_resources (id, skill_id, resource_id, is_primary, display_order, priority_score, created_at, updated_at)
                SELECT gen_random_uuid(), {newId}, resource_id, is_primary, display_order, priority_score, created_at, updated_at
                FROM skill_resources WHERE skill_id = {oldId}
                ON CONFLICT (skill_id, resource_id) DO NOTHING", ct);

            await _unitOfWork.ExecuteSqlAsync(
                $"DELETE FROM skill_resources WHERE skill_id = {oldId}", ct);

            // 3. Move prerequisites (both directions), guard against self-loops after merge.
            int movedPrereqsForward = await _unitOfWork.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO skill_prerequisites (skill_id, prerequisite_id)
                SELECT {newId}, prerequisite_id FROM skill_prerequisites
                  WHERE skill_id = {oldId} AND prerequisite_id <> {newId}
                ON CONFLICT (skill_id, prerequisite_id) DO NOTHING", ct);

            int movedPrereqsReverse = await _unitOfWork.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO skill_prerequisites (skill_id, prerequisite_id)
                SELECT skill_id, {newId} FROM skill_prerequisites
                  WHERE prerequisite_id = {oldId} AND skill_id <> {newId}
                ON CONFLICT (skill_id, prerequisite_id) DO NOTHING", ct);

            await _unitOfWork.ExecuteSqlAsync(
                $"DELETE FROM skill_prerequisites WHERE skill_id = {oldId} OR prerequisite_id = {oldId}", ct);

            // 4. Audit log — admin-supplied reason is parameterized, not interpolated as SQL.
            await _unitOfWork.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO admin_actions (id, admin_user_id, action_type, target_id, target_type, metadata, created_at)
                VALUES (gen_random_uuid(), {adminId}, 'merge_skill', {oldId}, 'skill',
                        jsonb_build_object('mergedInto', {newId}::text, 'reason', {reason}), NOW())", ct);

            // 5. Finally remove the duplicate skill row.
            await _unitOfWork.ExecuteSqlAsync(
                $"DELETE FROM skills WHERE id = {oldId}", ct);

            return new MergeSkillResponse
            {
                OldSkillId = oldId,
                NewSkillId = newId,
                MovedRoadmapNodes = movedRoadmapNodes,
                MovedJdSkills = movedJdSkills,
                MovedGapSkills = movedGapSkills,
                MovedResources = movedResources,
                MovedPrerequisites = movedPrereqsForward + movedPrereqsReverse
            };
        }, cancellationToken);
    }
}
