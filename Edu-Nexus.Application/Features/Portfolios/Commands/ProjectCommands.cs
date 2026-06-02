using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;
using System.Text.Json;
using System;

namespace Edu_Nexus.Application.Features.Portfolios.Commands;

// ADD
public record AddProjectCommand(AddProjectRequest Request) : IRequest<PortfolioProjectDto>;

public class AddProjectCommandHandler : IRequestHandler<AddProjectCommand, PortfolioProjectDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddProjectCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PortfolioProjectDto> Handle(AddProjectCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        // Quota check
        var subscription = await _unitOfWork.UserSubscriptions.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active,
            "Tier", cancellationToken);
            
        var quota = subscription?.Tier?.PortfolioProjectQuota ?? 3;
        
        if (quota >= 0)
        {
            var count = (await _unitOfWork.PortfolioProjects.FindAsync(p => p.UserId == userId, "", cancellationToken)).Count();
            if (count >= quota)
            {
                throw new Exception($"403 QUOTA_EXCEEDED|project|{count}|{quota}");
            }
        }

        var project = new PortfolioProject
        {
            UserId = userId,
            Title = command.Request.Title,
            Description = command.Request.Description,
            RepoUrl = command.Request.RepoUrl,
            LiveUrl = command.Request.LiveUrl,
            ImageUrl = command.Request.ImageUrl,
            TechStack = command.Request.TechStack != null ? JsonSerializer.Serialize(command.Request.TechStack) : null,
            Role = command.Request.Role,
            StartedDate = string.IsNullOrWhiteSpace(command.Request.StartedDate) ? null : DateOnly.Parse(command.Request.StartedDate),
            CompletedDate = string.IsNullOrWhiteSpace(command.Request.CompletedDate) ? null : DateOnly.Parse(command.Request.CompletedDate),
            IsVisible = command.Request.IsVisible,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWork.PortfolioProjects.Add(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PortfolioProjectDto
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            RepoUrl = project.RepoUrl,
            LiveUrl = project.LiveUrl,
            ImageUrl = project.ImageUrl,
            TechStack = command.Request.TechStack,
            Role = project.Role,
            StartedDate = project.StartedDate?.ToString("yyyy-MM-dd"),
            CompletedDate = project.CompletedDate?.ToString("yyyy-MM-dd"),
            IsVisible = project.IsVisible
        };
    }
}

// UPDATE
public record UpdateProjectCommand(Guid Id, AddProjectRequest Request) : IRequest<PortfolioProjectDto>;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, PortfolioProjectDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProjectCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PortfolioProjectDto> Handle(UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var project = await _unitOfWork.PortfolioProjects.FirstOrDefaultAsync(p => p.Id == command.Id && p.UserId == userId, "", cancellationToken)
            ?? throw new Exception("404 NOT_FOUND");

        project.Title = command.Request.Title;
        project.Description = command.Request.Description;
        project.RepoUrl = command.Request.RepoUrl;
        project.LiveUrl = command.Request.LiveUrl;
        project.ImageUrl = command.Request.ImageUrl;
        project.TechStack = command.Request.TechStack != null ? JsonSerializer.Serialize(command.Request.TechStack) : null;
        project.Role = command.Request.Role;
        project.StartedDate = string.IsNullOrWhiteSpace(command.Request.StartedDate) ? null : DateOnly.Parse(command.Request.StartedDate);
        project.CompletedDate = string.IsNullOrWhiteSpace(command.Request.CompletedDate) ? null : DateOnly.Parse(command.Request.CompletedDate);
        project.IsVisible = command.Request.IsVisible;

        _unitOfWork.PortfolioProjects.Update(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PortfolioProjectDto
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            RepoUrl = project.RepoUrl,
            LiveUrl = project.LiveUrl,
            ImageUrl = project.ImageUrl,
            TechStack = command.Request.TechStack,
            Role = project.Role,
            StartedDate = project.StartedDate?.ToString("yyyy-MM-dd"),
            CompletedDate = project.CompletedDate?.ToString("yyyy-MM-dd"),
            IsVisible = project.IsVisible
        };
    }
}

// DELETE
public record DeleteProjectCommand(Guid Id) : IRequest;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteProjectCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteProjectCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var project = await _unitOfWork.PortfolioProjects.FirstOrDefaultAsync(p => p.Id == command.Id && p.UserId == userId, "", cancellationToken)
            ?? throw new Exception("404 NOT_FOUND");

        _unitOfWork.PortfolioProjects.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// TOGGLE VISIBILITY
public record ToggleProjectVisibilityCommand(Guid Id, bool IsVisible) : IRequest<PortfolioProjectDto>;

public class ToggleProjectVisibilityCommandHandler : IRequestHandler<ToggleProjectVisibilityCommand, PortfolioProjectDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ToggleProjectVisibilityCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PortfolioProjectDto> Handle(ToggleProjectVisibilityCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var project = await _unitOfWork.PortfolioProjects.FirstOrDefaultAsync(p => p.Id == command.Id && p.UserId == userId, "", cancellationToken)
            ?? throw new Exception("404 NOT_FOUND");

        project.IsVisible = command.IsVisible;

        _unitOfWork.PortfolioProjects.Update(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PortfolioProjectDto
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            RepoUrl = project.RepoUrl,
            LiveUrl = project.LiveUrl,
            ImageUrl = project.ImageUrl,
            TechStack = string.IsNullOrWhiteSpace(project.TechStack) ? null : JsonSerializer.Deserialize<List<string>>(project.TechStack),
            Role = project.Role,
            StartedDate = project.StartedDate?.ToString("yyyy-MM-dd"),
            CompletedDate = project.CompletedDate?.ToString("yyyy-MM-dd"),
            IsVisible = project.IsVisible
        };
    }
}
