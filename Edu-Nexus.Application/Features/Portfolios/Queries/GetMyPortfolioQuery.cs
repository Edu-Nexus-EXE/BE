using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Portfolios;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.RoadmapNodes;
using MediatR;
using System.Text.Json;

namespace Edu_Nexus.Application.Features.Portfolios.Queries;

public record GetMyPortfolioQuery() : IRequest<PortfolioResponseData>;

public class GetMyPortfolioQueryHandler : IRequestHandler<GetMyPortfolioQuery, PortfolioResponseData>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPortfolioUrlBuilder _urlBuilder;

    public GetMyPortfolioQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPortfolioUrlBuilder urlBuilder)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _urlBuilder = urlBuilder;
    }

    public async Task<PortfolioResponseData> Handle(GetMyPortfolioQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId, "", cancellationToken)
            ?? throw new Exception("401 UNAUTHORIZED");

        var portfolio = await _unitOfWork.Portfolios.FirstOrDefaultAsync(p => p.UserId == userId, "", cancellationToken);

        if (portfolio == null)
        {
            // Auto create if not exist
            portfolio = new Portfolio
            {
                UserId = userId,
                ShowCompletedSkills = true,
                ShowCertificates = true,
                ShowProjects = true,
                IsPublic = false,
                UpdatedAt = DateTime.UtcNow
            };
            _unitOfWork.Portfolios.Add(portfolio);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var certificates = await _unitOfWork.PortfolioCertificates.FindAsync(c => c.UserId == userId, "", cancellationToken);
        var projects = await _unitOfWork.PortfolioProjects.FindAsync(p => p.UserId == userId, "", cancellationToken);

        var response = new PortfolioResponseData
        {
            UserId = userId,
            Slug = user.PortfolioUrlSlug,
            PortfolioUrl = _urlBuilder.Build(user.PortfolioUrlSlug),
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Headline = portfolio.Headline,
            Bio = portfolio.Bio,
            CoverImageUrl = portfolio.CoverImageUrl,
            ShowCompletedSkills = portfolio.ShowCompletedSkills,
            ShowCertificates = portfolio.ShowCertificates,
            ShowProjects = portfolio.ShowProjects,
            IsPublic = portfolio.IsPublic,
            UpdatedAt = portfolio.UpdatedAt,
            Certificates = new List<PortfolioCertificateDto>(),
            Projects = new List<PortfolioProjectDto>(),
            CompletedSkills = new List<CompletedSkillDto>()
        };

        if (portfolio.ShowCertificates)
        {
            response.Certificates = certificates.Select(c => new PortfolioCertificateDto
            {
                Id = c.Id,
                Name = c.Name,
                Issuer = c.Issuer,
                IssuedDate = c.IssuedDate?.ToString("yyyy-MM-dd"),
                ExpiresDate = c.ExpiresDate?.ToString("yyyy-MM-dd"),
                CredentialUrl = c.CredentialUrl,
                FileUrl = c.FileUrl,
                IsVisible = c.IsVisible
            }).ToList();
        }

        if (portfolio.ShowProjects)
        {
            response.Projects = projects.Select(p => new PortfolioProjectDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                RepoUrl = p.RepoUrl,
                LiveUrl = p.LiveUrl,
                ImageUrl = p.ImageUrl,
                TechStack = string.IsNullOrWhiteSpace(p.TechStack) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(p.TechStack) ?? new List<string>(),
                Role = p.Role,
                StartedDate = p.StartedDate?.ToString("yyyy-MM-dd"),
                CompletedDate = p.CompletedDate?.ToString("yyyy-MM-dd"),
                IsVisible = p.IsVisible
            }).ToList();
        }

        if (portfolio.ShowCompletedSkills)
        {
            var userNodes = await _unitOfWork.RoadmapNodes.FindAsync(
                n => n.Status == RoadmapNodeStatus.Completed
                     && n.Roadmap.UserId == userId
                     && n.Skill != null,
                "Skill,Roadmap", cancellationToken);

            response.CompletedSkills = userNodes
                .GroupBy(n => n.Skill!.Id)
                .Select(g =>
                {
                    var earliest = g
                        .OrderBy(n => n.CompletedAt ?? DateTime.MaxValue)
                        .First();
                    return new CompletedSkillDto
                    {
                        SkillId = earliest.Skill!.Id,
                        SkillName = earliest.Skill!.Name,
                        CompletedAt = earliest.CompletedAt,
                        FromRoadmap = earliest.Roadmap?.Title
                    };
                })
                .OrderByDescending(s => s.CompletedAt ?? DateTime.MinValue)
                .ToList();
        }

        return response;
    }
}
