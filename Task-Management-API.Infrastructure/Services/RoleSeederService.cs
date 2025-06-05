﻿using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task_Management_Api.Application.DTO;
using Task_Management_Api.Application.Interfaces;
using Task_Management_API.Domain.Constants;
using Task_Management_API.Domain.Models;

namespace Task_Management_API.Infrastructure.Services
{
    public class RoleSeederService : IRoleSeederService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IOptions<AdminSetting> _adminSettings;
        private readonly ILogger<RoleSeederService> _logger;

        public RoleSeederService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<AdminSetting> adminSettings,
            ILogger<RoleSeederService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _adminSettings = adminSettings;
            _logger = logger;
        }

        public async Task SeedRolesAndAdminAsync()
        {
            try
            {
                // Create Roles if they don't exist
                await CreateRoleIfNotExistsAsync(Roles.Admin);
                await CreateRoleIfNotExistsAsync(Roles.User);
                await CreateRoleIfNotExistsAsync(Roles.Manager);

                // Create Admin Users
                await CreateAdminUsersAsync();

                _logger.LogInformation("Roles and admin users seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding roles and admin users");
                throw;
            }
        }

        private async Task CreateRoleIfNotExistsAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole(roleName);
                var result = await _roleManager.CreateAsync(role);

                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
                else
                {
                    _logger.LogInformation("Role {RoleName} created successfully", roleName);
                }
            }
        }

        private async Task CreateAdminUsersAsync()
        {
            var adminEmails = _adminSettings.Value.AdminEmails;
            var defaultPassword = _adminSettings.Value.DefaultAdminPassword;

            foreach (var email in adminEmails)
            {
                await CreateAdminUserAsync(email, defaultPassword);
            }
        }

        private async Task CreateAdminUserAsync(string email, string password)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    Country = "System" // or any default value
                };

                var result = await _userManager.CreateAsync(adminUser, password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, Roles.Admin);
                    _logger.LogInformation("Admin user {Email} created successfully", email);
                }
                else
                {
                    _logger.LogError("Failed to create admin user {Email}: {Errors}",
                        email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Make sure existing user has admin role
                if (!await _userManager.IsInRoleAsync(existingUser, Roles.Admin))
                {
                    await _userManager.AddToRoleAsync(existingUser, Roles.Admin);
                    _logger.LogInformation("Added admin role to existing user {Email}", email);
                }
            }
        }
    }
}
