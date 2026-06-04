using Edu_Nexus.Application.Interfaces.Portfolios;
using Microsoft.Extensions.Configuration;

namespace Edu_Nexus.Infrastructure.Portfolios;

public class PortfolioUrlBuilder : IPortfolioUrlBuilder
{
    private readonly string _baseUrl;

    public PortfolioUrlBuilder(IConfiguration configuration)
    {
        _baseUrl = (configuration["App:PublicBaseUrl"] ?? "https://edunexus.vn").TrimEnd('/');
    }

    public string? Build(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        return $"{_baseUrl}/p/{slug}";
    }
}
