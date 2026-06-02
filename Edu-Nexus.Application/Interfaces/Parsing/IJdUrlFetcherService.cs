using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface IJdUrlFetcherService
{
    Task<string> FetchAsync(string url, CancellationToken cancellationToken = default);
}
