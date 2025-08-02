using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Task_Management_Api.Application.DTO;
using Task_Management_Api.Application.Interfaces;
using Task_Management_API.Domain.Constants;
using Task_Management_Api.Application.Pagination;
using Task_Management_API.Domain.Models;


namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskRepository _taskRepo;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<TaskController> _logger;

        public TaskController(ITaskRepository taskRepository, IUserRepository userRepository,
            ILogger<TaskController> logger)
        {
            _taskRepo = taskRepository;
            _userRepo = userRepository;
            _logger = logger;
        }

        // Get All Tasks For Admins - Only Admins can see all tasks across the system
        [HttpGet("GetAllTasks")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAllTasks([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                var paginatedTasks = await _taskRepo.GetAllPaginationAsync(
                    paginationParams.PageNumber,
                    paginationParams.ValidatedPageSize,
                    includeUser: true);

                if (!paginatedTasks.Items.Any())
                {
                    _logger.LogInformation("No tasks found.");
                    return Ok(new { Message = "No tasks found.", Tasks = new List<AppTask>() });
                }

                var tasksDto = paginatedTasks.Items.Select(t => new TaskWithUserDto
                {
                    Id = t.Id,
                    Description = t.Description,
                    Title = t.Title,
                    Status = t.Status,
                    UserName = t.User?.UserName 
                }).ToList();

                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(new
                {
                    paginatedTasks.TotalCount,
                    paginatedTasks.PageSize,
                    paginatedTasks.CurrentPage,
                    paginatedTasks.TotalPages
                }));

                var response = new
                {
                    Message = "Tasks retrieved successfully.",
                    Tasks = tasksDto,
                    PageInfo = new
                    {
                        paginatedTasks.CurrentPage,
                        paginatedTasks.PageSize,
                        paginatedTasks.TotalPages,
                        paginatedTasks.TotalCount,
                    }
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated tasks.");
                return StatusCode(500, new { Message = "Server error occurred." });
            }
        }

        [HttpGet("MyTasks")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User)]
        public async Task<IActionResult> GetUserTasks([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetUserTasks: User ID could not be determined from token.");
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }

                if (!await _userRepo.UserExistsAsync(userId))
                {
                    _logger.LogWarning($"GetUserTasks: User with ID {userId} not found.");
                    return NotFound(new ErrorResponse { Message = "User not found." });
                }
                var paginatedTasks = await _taskRepo.GetUserTasksPaginationAsync(userId, paginationParams.PageNumber, paginationParams.PageSize);

                var response = new
                {
                    Message = paginatedTasks.Items.Any() ? "Tasks retrieved successfully." : "No tasks found for this user.",
                    Tasks = paginatedTasks.Items,
                    Pagination = new
                    {
                        paginatedTasks.CurrentPage,
                        paginatedTasks.PageSize,
                        paginatedTasks.TotalPages
                    }
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for user ");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while retrieving your tasks." });
            }
        }
        // Get specific task by ID
        [HttpGet("SpecificTask/{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User)]
        public async Task<IActionResult> GetTaskById(int id)
        {
            try
            {
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetTaskById: User ID could not be determined from token.");
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }
                AppTask? task = null;
                if (await _userRepo.IsUserInRoleAsync(userId, Roles.Admin))
                {
                    task = await _taskRepo.GetByIdAsync(id);
                }
                else
                {
                    task = await _taskRepo.GetTaskByIdAsync(id, userId);
                }

                if (task == null)
                {
                    return NotFound(new ErrorResponse { Message = $"Task with ID {id} not found or you don't have permission to access it." });
                }

                var taskInfo = new TaskInformation
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Status = task.Status,
                    DueDate = task.DueDate
                };

                var response = new { Message = "Task retrieved successfully.", Task = taskInfo };
                _logger.LogInformation("Task {TaskId} retrieved successfully for user {UserId}.", id, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task with ID {TaskId} for user {UserId}.", id, _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while retrieving the task." });
            }
        }

        // Add a new Task
        [HttpPost("Add")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User)]
        public async Task<IActionResult> AddTask([FromBody] TaskInformation taskFromRequest)
        {
            try
            {
                if (taskFromRequest == null)
                {
                    return BadRequest(new ErrorResponse { Message = "Task data is required." });
                }
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ErrorResponse { Message = "Invalid task data.", Errors = errors });
                }

                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }
                if (!await _userRepo.UserExistsAsync(userId))
                {
                    return NotFound(new ErrorResponse { Message = "User not found." });
                }

                var task = new AppTask
                {
                    Title = taskFromRequest.Title,
                    Description = taskFromRequest.Description,
                    Status = taskFromRequest.Status,
                    DueDate = taskFromRequest.DueDate,
                    UserId = userId
                };

                await _taskRepo.AddAsync(task);
                await _taskRepo.SaveAsync();

                // Reload the task from the database to get the generated Id
                var savedTask = await _taskRepo.GetByIdAsync(task.Id);

                return CreatedAtAction(
                    nameof(GetTaskById),
                    new { id = savedTask.Id },
                    new TaskResponse
                    {
                        Message = "Task created successfully.",
                        Task = new TaskInformation
                        {
                            Id = savedTask.Id,
                            Title = savedTask.Title,
                            Description = savedTask.Description,
                            Status = savedTask.Status,
                            DueDate = savedTask.DueDate
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding task for user {UserId}.", _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while creating the task." });
            }
        }

        // Update an existing Task
        [HttpPut("Update/{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User)]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskInformation taskFromRequest)
        {
            try
            {
                if (taskFromRequest == null)
                {
                    return BadRequest(new ErrorResponse { Message = "Task data is required." });
                }
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ErrorResponse { Message = "Invalid task data.", Errors = errors });
                }

                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }

                AppTask? taskFromDatabase = null;
                if (await _userRepo.IsUserInRoleAsync(userId, Roles.Admin))
                {
                    taskFromDatabase = await _taskRepo.GetByIdAsync(id);
                }
                else
                {
                    taskFromDatabase = await _taskRepo.GetTaskByIdAsync(id, userId);
                }

                if (taskFromDatabase == null)
                {
                    return NotFound(new ErrorResponse { Message = $"Task with ID {id} not found or you don't have permission to update it." });
                }

                taskFromDatabase.Title = taskFromRequest.Title;
                taskFromDatabase.Description = taskFromRequest.Description;
                taskFromDatabase.Status = taskFromRequest.Status;
                taskFromDatabase.DueDate = taskFromRequest.DueDate;

                _taskRepo.UpdateAsync(taskFromDatabase);
                await _taskRepo.SaveAsync();

                _logger.LogInformation("Task {TaskId} updated successfully by user {UserId}", id, userId);

                var updatedTaskInfo = new TaskInformation
                {
                    Id = taskFromDatabase.Id,
                    Title = taskFromDatabase.Title,
                    Description = taskFromDatabase.Description,
                    Status = taskFromDatabase.Status,
                    DueDate = taskFromDatabase.DueDate
                };

                return Ok(new { Message = "Task updated successfully.", Task = updatedTaskInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating task {TaskId} for user {UserId}.", id, _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while updating the task." });
            }
        }

        // Delete a Task
        [HttpDelete("Delete/{taskId}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User)]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            try
            {
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }

                bool deleted = false;
                string taskOwnerId = userId; // Default to current user

                if (await _userRepo.IsUserInRoleAsync(userId, Roles.Admin))
                {
                    var taskToDelete = await _taskRepo.GetByIdAsync(taskId);
                    if (taskToDelete != null)
                    {
                        taskOwnerId = taskToDelete.UserId; // Get actual owner for cache invalidation
                        _taskRepo.DeleteAsync(taskToDelete);
                        await _taskRepo.SaveAsync();
                        deleted = true;
                    }
                }
                else
                {
                    deleted = await _taskRepo.DeleteTaskByIdAsync(taskId, userId);
                }

                if (!deleted)
                {
                    return NotFound(new ErrorResponse { Message = $"Task with ID {taskId} not found or you don't have permission to delete it." });
                }
                _logger.LogInformation("Task {TaskId} deleted successfully by user {UserId}", taskId, userId);

                return Ok(new { Message = $"Task with ID {taskId} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting task {TaskId} for user {UserId}.", taskId, _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while deleting the task." });
            }
        }

        // Get task count for current user
        [HttpGet("Count")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User)]
        public async Task<IActionResult> GetTaskCount()
        {
            try
            {
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }

                var count = await _taskRepo.GetUserTaskCountAsync(userId);
                var response = new { Message = "Task count retrieved successfully.", Count = count };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task count for user {UserId}.", _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while retrieving task count." });
            }
        }
    }
}