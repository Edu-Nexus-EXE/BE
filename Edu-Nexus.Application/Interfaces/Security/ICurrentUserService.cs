namespace Edu_Nexus.Application.Interfaces.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
}
