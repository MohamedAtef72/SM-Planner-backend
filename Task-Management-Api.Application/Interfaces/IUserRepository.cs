using Microsoft.AspNetCore.Identity;
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
    public interface IUserRepository
    {
        // Get Operations
        Task<List<UserInformation>> GetAllUsersAsync();
        Task<PaginationListHelper<UserInformation>> GetAllPaginationAsync(int pageNumber, int pageSize);

        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        string? GetUserIdFromJwtClaims();

        // CRUD Operations (specific to Identity)
        Task<IdentityResult> AddUserAsync(UserRegister userRegister);
        Task<IdentityResult> UpdateUserAsync(string userId, UserInformation updatedUser);
        Task<IdentityResult> DeleteUserAsync(string userId);
        Task<bool> UserExistsAsync(string userId);

        // Save changes
        Task SaveAsync();
        //Task SavePath(string imageUrl);

        // New Role Management Methods
        Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName);
        Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<bool> IsUserInRoleAsync(string userId, string roleName);
    }
}
