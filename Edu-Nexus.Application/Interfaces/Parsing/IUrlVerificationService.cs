namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface IUrlVerificationService
{
    /// <summary>
    /// Verify URL is accessible by sending a HEAD request with timeout.
    /// Returns true if URL responds with 2xx/3xx status, false if 4xx/5xx or timeout.
    /// </summary>
    Task<bool> IsValidUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify multiple URLs in parallel. Returns dict of URL -> is_valid.
    /// </summary>
    Task<Dictionary<string, bool>> VerifyUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
}
