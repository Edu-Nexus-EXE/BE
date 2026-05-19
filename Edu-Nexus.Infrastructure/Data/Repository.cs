using Edu_Nexus.Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Edu_Nexus.Infrastructure.Data;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly EduNexusDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(EduNexusDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, string includeProperties = "", CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Include(includeProperty);
        }
        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(string includeProperties = "", CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Include(includeProperty);
        }
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, string includeProperties = "", CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Include(includeProperty);
        }
        return await query.Where(predicate).ToListAsync(cancellationToken);
    }

    public void Add(T entity)
    {
        _dbSet.Add(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }
}
