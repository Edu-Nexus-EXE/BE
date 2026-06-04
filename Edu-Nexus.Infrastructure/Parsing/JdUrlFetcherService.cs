using Edu_Nexus.Application.Interfaces.Parsing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Edu_Nexus.Infrastructure.Parsing;

public class JdUrlFetcherService : IJdUrlFetcherService
{
    private readonly HttpClient _httpClient;

    public JdUrlFetcherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(url, cancellationToken);
            // Very naive HTML to text extraction for JD parsing
            var noScript = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            var noStyle = Regex.Replace(noScript, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
            var noTags = Regex.Replace(noStyle, @"<[^>]+>", " ");
            var normalizedSpaces = Regex.Replace(noTags, @"\s+", " ");
            return normalizedSpaces.Trim();
        }
        catch
        {
            return string.Empty;
        }
    }
}
