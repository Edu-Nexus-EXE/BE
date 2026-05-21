using Edu_Nexus.Application.Interfaces.Storage;
using Microsoft.AspNetCore.Hosting;

namespace Edu_Nexus.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorage(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveAsync(Stream content, string subfolder, string fileExtension, CancellationToken cancellationToken = default)
    {
        var webRoot = ResolveWebRoot();
        var folder = Path.Combine(webRoot, subfolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{fileExtension}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var fs = File.Create(fullPath))
        {
            await content.CopyToAsync(fs, cancellationToken);
        }

        var normalizedSub = subfolder.Replace('\\', '/').Trim('/');
        return $"/{normalizedSub}/{fileName}";
    }

    public Task<Stream> OpenReadAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveFullPath(fileUrl);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Stored file is missing.", fileUrl);
        }
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;
        var fullPath = ResolveFullPath(fileUrl);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    private string ResolveWebRoot()
    {
        if (!string.IsNullOrEmpty(_env.WebRootPath))
        {
            return _env.WebRootPath;
        }
        var fallback = Path.Combine(_env.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private string ResolveFullPath(string fileUrl)
    {
        var relative = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(ResolveWebRoot(), relative);
    }
}
