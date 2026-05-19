using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;

namespace Edu_Nexus.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly EduNexusDbContext _context;

    public UnitOfWork(EduNexusDbContext context)
    {
        _context = context;
        Users = new Repository<User>(_context);
        RefreshTokens = new Repository<RefreshToken>(_context);
        SubscriptionTiers = new Repository<SubscriptionTier>(_context);
        UserSubscriptions = new Repository<UserSubscription>(_context);
    }

    public IRepository<User> Users { get; private set; }
    public IRepository<RefreshToken> RefreshTokens { get; private set; }
    public IRepository<SubscriptionTier> SubscriptionTiers { get; private set; }
    public IRepository<UserSubscription> UserSubscriptions { get; private set; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
