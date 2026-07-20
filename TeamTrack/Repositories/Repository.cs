using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TeamTrack.Data;

namespace TeamTrack.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _context;

        public Repository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
        }

        public void Remove(T entity)
        {
            _context.Set<T>().Remove(entity);
        }

        public void Detach(T entity)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }

        public void ClearTracker()
        {
            _context.ChangeTracker.Clear();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public IQueryable<T> Query()
        {
            return _context.Set<T>();
        }

        public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(predicate);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);
        }
    }
}