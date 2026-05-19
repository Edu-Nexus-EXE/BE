using System.Linq.Expressions;

namespace Edu_Nexus.Application.Interfaces.Data;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, string includeProperties = "", CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(string includeProperties = "", CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, string includeProperties = "", CancellationToken cancellationToken = default);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
