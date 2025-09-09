using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task_Management_Api.Application.DTO;
using Task_Management_Api.Application.Pagination;
using Task_Management_API.Domain.Models;

namespace Task_Management_Api.Application.Interfaces
{
    public interface ITaskRepository : IMainRepository<AppTask>
    {
        // Specific Get Operations
        Task<PaginationListHelper<TaskInformation>> GetUserTasksPaginationAsync(string userId, int pageNumber, int pageSize , string status);
        Task<AppTask?> GetTaskByIdAsync(int taskId, string userId); // Added userId for security
        Task<List<AppTask>> GetUserTasksEntitiesAsync(string userId); // Returns Tasks entities instead of DTO

        // Utility Operations
        Task<bool> IsTaskOwnedByUserAsync(int taskId, string userId);
        Task<int> GetUserTaskCountAsync(string userId);
        Task<bool> DeleteTaskByIdAsync(int taskId, string userId); // User-specific delete
    }
}
