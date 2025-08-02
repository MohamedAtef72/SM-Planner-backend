using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Task_Management_Api.Application.DTO;
using Task_Management_Api.Application.Interfaces;
using Task_Management_Api.Application.Pagination;
using Task_Management_API.Domain.Models;
using Task_Management_API.Infrastructure.Data;

namespace Task_Management_API.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserRepository(UserManager<ApplicationUser> userManager, AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        // Get all users Pagination
        public async Task<PaginationListHelper<UserInformation>> GetAllPaginationAsync(int pageNumber, int pageSize)
        {
            // Step 1: Get paginated users without roles
            var query = _userManager.Users
                .Select(user => new UserInformation
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Country = user.Country
                })
                .AsNoTracking();

            var pagedUsers = await PaginationListHelper<UserInformation>.CreateAsync(query, pageNumber, pageSize);

            // Step 2: Fill roles for each user
            foreach (var userInfo in pagedUsers.Items)
            {
                var user = await _userManager.FindByNameAsync(userInfo.UserName);
                userInfo.Role = (await _userManager.GetRolesAsync(user)).ToList();
            }

            return pagedUsers;

        }


        // Get all users
        public async Task<List<UserInformation>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var usersInformation = users.Select(user => new UserInformation
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country
            }).ToList();

            return usersInformation;
        }

        // Get specific user by ID
        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        // Add new user
        public async Task<IdentityResult> AddUserAsync(UserRegister userRegister)
        {
            var user = new ApplicationUser
            {
                UserName = userRegister.UserName,
                Email = userRegister.Email,
                PhoneNumber = userRegister.PhoneNumber,
                Country = userRegister.Country
            };

            var result = await _userManager.CreateAsync(user, userRegister.Password);
            return result;
        }

        // Update existing user
        public async Task<IdentityResult> UpdateUserAsync(string userId, UserInformation updatedUser)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            user.UserName = updatedUser.UserName;
            user.Email = updatedUser.Email;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Country = updatedUser.Country;

            if (!string.IsNullOrEmpty(updatedUser.ImagePath))
            {
                user.ImagePath = updatedUser.ImagePath;
            }

            var result = await _userManager.UpdateAsync(user);
            return result;
        }


        // Delete user
        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = $"User with ID {userId} not found."
                });
            }

            return await _userManager.DeleteAsync(user);
        }

        // Check if user exists
        public async Task<bool> UserExistsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null;
        }

        // Get current user ID from JWT claims
        public string? GetUserIdFromJwtClaims()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
            if (claimsPrincipal == null)
                return null;

            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim?.Value;
        }

        // Save changes (Identity handles its own context)
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
        // Role Management Methods (as provided by you)
        public async Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }
            return await _userManager.AddToRoleAsync(user, roleName);
        }

        public async Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }
            return await _userManager.RemoveFromRoleAsync(user, roleName);
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new List<string>();
            }
            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<bool> IsUserInRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }
            return await _userManager.IsInRoleAsync(user, roleName);
        }

        public async Task<List<UserWithRoles>> GetUsersWithRolesAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var usersWithRoles = new List<UserWithRoles>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new UserWithRoles
                {
                    Id = user.Id, // user.Id is already a string
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Country = user.Country,
                    Roles = roles.ToList()
                });
            }
            return usersWithRoles;
        }
    }
}
