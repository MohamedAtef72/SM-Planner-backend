using Microsoft.EntityFrameworkCore;
using Task_Management_Api.Application.Interfaces;
using Task_Management_Api.Application.Pagination;
using Task_Management_API.Infrastructure.Data;

namespace Task_Management_API.Infrastructure.Repositories
{
    public class BaseRepository<T> : IMainRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<PaginationListHelper<T>> GetAllPaginationAsync(int pageNumber, int pageSize , bool includeUser)
        {
            IQueryable<T> query = _dbSet.Include("User"); ;
            return await PaginationListHelper<T>.CreateAsync(query, pageNumber, pageSize);
        }


        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }
        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await Task.CompletedTask; // Keep it async for consistency
        }

        public virtual async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await Task.CompletedTask; // Keep it async for consistency
        }

        public virtual async Task<bool> DeleteByIdAsync(int id)
        {
    var entity = await GetByIdAsync(id);
    if (entity == null)
        return false;

    _dbSet.Remove(entity);
    await _context.SaveChangesAsync(); // ✅ This line is necessary
    return true;
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            return entity != null;
        }

        public virtual async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
