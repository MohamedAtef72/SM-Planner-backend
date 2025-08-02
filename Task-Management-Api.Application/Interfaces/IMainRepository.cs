using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task_Management_Api.Application.Pagination;

namespace Task_Management_Api.Application.Interfaces
{
    public interface IMainRepository<T> where T : class
    {
        // Generic CRUD Operations
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<PaginationListHelper<T>> GetAllPaginationAsync(int pageNumber, int pageSize, bool includeUser);

        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<bool> DeleteByIdAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task SaveAsync();
    }
}
