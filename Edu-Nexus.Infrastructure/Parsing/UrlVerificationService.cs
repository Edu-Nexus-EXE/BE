using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Parsing;

public class UrlVerificationService : IUrlVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UrlVerificationService> _logger;
    private const int TimeoutSeconds = 10;

    public UrlVerificationService(HttpClient httpClient, ILogger<UrlVerificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
    }

    public async Task<bool> IsValidUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url);
            var request = new HttpRequestMessage(HttpMethod.Head, uri);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            var isValid = response.IsSuccessStatusCode || (int)response.StatusCode < 400;
            if (!isValid)
            {
                _logger.LogWarning("URL verification failed for {Url}: {StatusCode}", url, response.StatusCode);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "URL verification error for {Url}", url);
            return false;
        }
    }

    public async Task<Dictionary<string, bool>> VerifyUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        var tasks = urls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct()
            .Select(async url => new { url, isValid = await IsValidUrlAsync(url, cancellationToken) })
            .ToList();

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.url, r => r.isValid);
    }
}
