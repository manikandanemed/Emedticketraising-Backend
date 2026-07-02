using System.Linq.Expressions;

namespace TeamTrack.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task AddAsync(T entity);
        void Remove(T entity);
        Task SaveAsync();
        IQueryable<T> Query();
        Task<T?> GetAsync(Expression<Func<T, bool>> predicate);
        Task AddRangeAsync(IEnumerable<T> entities);
    }
}