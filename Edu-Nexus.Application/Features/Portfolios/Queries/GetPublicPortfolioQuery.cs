using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.RoadmapNodes;
using MediatR;
using System.Text.Json;

namespace Edu_Nexus.Application.Features.Portfolios.Queries;

public record GetPublicPortfolioQuery(string Slug) : IRequest<PortfolioResponseData>;

public class GetPublicPortfolioQueryHandler : IRequestHandler<GetPublicPortfolioQuery, PortfolioResponseData>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPublicPortfolioQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PortfolioResponseData> Handle(GetPublicPortfolioQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new Exception("404 NOT_FOUND");
        }

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.PortfolioUrlSlug == request.Slug, "", cancellationToken)
            ?? throw new Exception("404 NOT_FOUND");

        var portfolio = await _unitOfWork.Portfolios.FirstOrDefaultAsync(p => p.UserId == user.Id, "", cancellationToken)
            ?? throw new Exception("404 NOT_FOUND");

        if (!portfolio.IsPublic)
        {
            throw new Exception("404 NOT_FOUND");
        }

        var certificates = await _unitOfWork.PortfolioCertificates.FindAsync(c => c.UserId == user.Id && c.IsVisible, "", cancellationToken);
        var projects = await _unitOfWork.PortfolioProjects.FindAsync(p => p.UserId == user.Id && p.IsVisible, "", cancellationToken);

        var response = new PortfolioResponseData
        {
            UserId = user.Id,
            Slug = user.PortfolioUrlSlug,
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
            var nodes = await _unitOfWork.RoadmapNodes.FindAsync(n => n.Status == RoadmapNodeStatus.Completed, "Skill,Roadmap", cancellationToken);
            var userNodes = nodes.Where(n => n.Roadmap != null && n.Roadmap.UserId == user.Id).ToList();
            
            var distinctSkills = userNodes
                .Where(n => n.Skill != null)
                .Select(n => n.Skill)
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .ToList();

            response.CompletedSkills = distinctSkills.Select(s => new CompletedSkillDto
            {
                SkillId = s.Id,
                Name = s.Name
            }).ToList();
        }

        return response;
    }
}
