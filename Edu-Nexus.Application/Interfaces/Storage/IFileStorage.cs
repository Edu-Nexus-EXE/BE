namespace Edu_Nexus.Application.Interfaces.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string subfolder, string fileExtension, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
}
