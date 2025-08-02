using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Task_Management_Api.Application.DTO;
using Task_Management_Api.Application.Interfaces;
using Task_Management_API.Domain.Constants;
using Task_Management_Api.Application.Pagination;
using Task_Management_API.Domain.Models;
using System.Text.Json;

namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepo; 
        private readonly UserManager<ApplicationUser> _userManager; 
        private readonly ILogger<UserController> _logger; 

        public UserController(IUserRepository userRepository, UserManager<ApplicationUser> userManager, ILogger<UserController> logger)
        {
            _userRepo = userRepository;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("GetRole")]
        public async Task<IActionResult> GetRole()
        {
            var userId = _userRepo.GetUserIdFromJwtClaims();

            if (userId == null)
            {
                return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
            }

            var user = await _userRepo.GetUserByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
            }

            var role = await _userManager.GetRolesAsync(user);
            if (role != null)
            {
                var response = new { Message = "User retrieved successfully.", Role = role };
                return Ok(response);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet("UserProfile")]
        public async Task<IActionResult> UserProfile()
        {
            var userId = _userRepo.GetUserIdFromJwtClaims();

            if (userId == null)
            {
                return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
            }

            var user = await _userRepo.GetUserByIdAsync(userId);

            if(user == null)
            {
                return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
            }

            var role = await _userManager.GetRolesAsync(user);

            var userInfo = new UserInformation
            {
                UserName = user.UserName,
                Email = user.Email,
                Country = user.Country,
                PhoneNumber = user.PhoneNumber,
                ImagePath = user.ImagePath,
                Role = (List<string>)role
            };

            var response = new { Message = "User retrieved successfully.", User = userInfo};

            return Ok(response);
        }

        // Get all users - Typically restricted to Admin or Manager roles for security
        [HttpGet("GetAllUsers")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Get([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                var paginatedUsers = await _userRepo.GetAllPaginationAsync(paginationParams.PageNumber, paginationParams.PageSize);

                if (paginatedUsers == null || !paginatedUsers.Items.Any())
                {
                    _logger.LogInformation("No users found in the system.");
                    return Ok(new { Message = "No users found.", Users = new List<UserInformation>() });
                }

                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(new
                {
                    paginatedUsers.TotalCount,
                    paginatedUsers.PageSize,
                    paginatedUsers.CurrentPage,
                    paginatedUsers.TotalPages
                }));

                var response = new
                {
                    Message = "Users retrieved successfully.",
                    Users = paginatedUsers.Items,
                    PageInfo = new
                    {
                        paginatedUsers.CurrentPage,
                        paginatedUsers.PageSize,
                        paginatedUsers.TotalCount,
                        paginatedUsers.TotalPages
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated users.");
                return StatusCode(500, new { Message = "Server error occurred." });
            }
        }



        [HttpPut("Update")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User)]
        public async Task<IActionResult> Update([FromForm] UserEditProfile updatedUser)
        {
            try
            {
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized attempt to update user profile.");
                    return Unauthorized(new ErrorResponse { Message = "User ID not found in JWT claims." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ErrorResponse { Message = "Invalid user data.", Errors = errors });
                }

                string? imagePath = null;

                if (updatedUser.Image != null)
                {
                    var imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(updatedUser.Image.FileName);
                    var imageFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");

                    if (!Directory.Exists(imageFolderPath))
                        Directory.CreateDirectory(imageFolderPath);

                    var imageFullPath = Path.Combine(imageFolderPath, imageFileName);

                    using (var stream = new FileStream(imageFullPath, FileMode.Create))
                    {
                        await updatedUser.Image.CopyToAsync(stream);
                    }

                    imagePath = $"Images/{imageFileName}"; 
                }

                var newUser = new UserInformation()
                {
                    UserName = updatedUser.UserName,
                    Email = updatedUser.Email,
                    PhoneNumber = updatedUser.PhoneNumber,
                    Country = updatedUser.Country,
                    Role = [updatedUser.Role[0]],
                    ImagePath = imagePath
                };

                var result = await _userRepo.UpdateUserAsync(userId, newUser);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Failed to update user {UserId}. Errors: {Errors}", userId, string.Join(", ", errors));
                    return BadRequest(new ErrorResponse { Message = "Update failed.", Errors = errors });
                }

                _logger.LogInformation("User {UserId} updated successfully.", userId);
                return Ok(new { Message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}.", _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred." });
            }
        }



        [HttpDelete("Delete")]
        [Authorize(Roles = Roles.User)]
        public async Task<IActionResult> Delete()
        {
            try
            {
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized attempt to delete user.");
                    return Unauthorized(new ErrorResponse { Message = "User ID not found in JWT claims." });
                }

                var result = await _userRepo.DeleteUserAsync(userId);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Failed to delete user {UserId}. Errors: {Errors}", userId, string.Join(", ", errors));
                    return BadRequest(new ErrorResponse { Message = "Delete failed.", Errors = errors });
                }

                _logger.LogInformation("User {UserId} deleted successfully.", userId);
                return Ok(new { Message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}.", _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred." });
            }
        }


        // Admin-specific endpoint to delete any user by ID (example)
        [HttpDelete("AdminDelete/{userId}")]
        [Authorize(Roles = Roles.Admin)] // Only Admin can delete any user by ID
        public async Task<IActionResult> AdminDeleteUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new ErrorResponse { Message = "User ID is required." });
                }

                var result = await _userRepo.DeleteUserAsync(userId);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Admin failed to delete user {UserId}. Errors: {Errors}", userId, string.Join(", ", errors));
                    return BadRequest(new ErrorResponse { Message = $"Failed to delete user with ID {userId}.", Errors = errors });
                }

                _logger.LogInformation("Admin deleted user {UserId} successfully.", userId);
                return Ok(new { Message = $"User with ID {userId} deleted successfully by admin." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId} by admin.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while deleting the user." });
            }
        }
    }
}