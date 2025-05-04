using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReemRPG.Models;
using Microsoft.EntityFrameworkCore;

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RolesController> _logger;

        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, ILogger<RolesController> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger; // Assign logger
        }

        // GET: api/Roles
        [HttpGet]
        public IActionResult GetRoles()
        {
            _logger.LogInformation("Fetching all roles.");
            var roles = _roleManager.Roles.ToList();
            return Ok(roles);
        }

        // GET: api/Roles/users
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]  // Only admins can access this endpoint
        public async Task<IActionResult> GetUsers()
        {
            _logger.LogInformation("Admin requested list of all users");

            var users = await _userManager.Users.ToListAsync();
            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    id = user.Id,
                    email = user.Email,
                    userName = user.UserName,
                    emailConfirmed = user.EmailConfirmed,
                    roles = roles
                });
            }

            return Ok(userList);
        }

        // GET: api/Roles/{roleId}
        [HttpGet("{roleId}")]
        public async Task<IActionResult> GetRole(string roleId)
        {
            _logger.LogInformation("Fetching role with ID: {RoleId}", roleId);

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role with ID {RoleId} not found.", roleId);
                return NotFound("Role not found.");
            }

            return Ok(role);
        }

        // POST: api/Roles
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] string roleName)
        {
            _logger.LogInformation("Attempting to create role: {RoleName}", roleName);

            if (string.IsNullOrWhiteSpace(roleName))
            {
                _logger.LogWarning("Role creation failed: Role name is empty.");
                return BadRequest("Role name cannot be empty.");
            }

            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (roleExists)
            {
                _logger.LogWarning("Role creation failed: Role '{RoleName}' already exists.", roleName);
                return BadRequest("Role already exists.");
            }

            var role = new IdentityRole(roleName);
            var result = await _roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                _logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                return Ok("Role created successfully.");
            }

            _logger.LogError("Error occurred while creating role '{RoleName}': {Errors}", roleName, result.Errors);
            return BadRequest(result.Errors);
        }

        // PUT: api/Roles
        [HttpPut]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleModel model)
        {
            _logger.LogInformation("Attempting to update role with ID: {RoleId}", model.RoleId);

            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null)
            {
                _logger.LogWarning("Role update failed: Role with ID {RoleId} not found.", model.RoleId);
                return NotFound("Role not found.");
            }

            role.Name = model.NewRoleName;
            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
            {
                _logger.LogInformation("Role with ID {RoleId} updated successfully.", model.RoleId);
                return Ok("Role updated successfully.");
            }

            _logger.LogError("Error occurred while updating role with ID {RoleId}: {Errors}", model.RoleId, result.Errors);
            return BadRequest(result.Errors);
        }

        // DELETE: api/Roles/{roleId}
        [HttpDelete("{roleId}")]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            _logger.LogInformation("Attempting to delete role with ID: {RoleId}", roleId);

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role deletion failed: Role with ID {RoleId} not found.", roleId);
                return NotFound("Role not found.");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Role with ID {RoleId} deleted successfully.", roleId);
                return Ok("Role deleted successfully.");
            }

            _logger.LogError("Error occurred while deleting role with ID {RoleId}: {Errors}", roleId, result.Errors);
            return BadRequest(result.Errors);
        }

        // POST: api/Roles/assign-role-to-user
        [HttpPost("assign-role-to-user")]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleModel model)
        {
            _logger.LogInformation("Assigning role '{RoleName}' to user '{UserId}'", model.RoleName, model.UserId);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.LogWarning("Role assignment failed: User with ID {UserId} not found.", model.UserId);
                return NotFound("User not found.");
            }

            var roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
            if (!roleExists)
            {
                _logger.LogWarning("Role assignment failed: Role '{RoleName}' does not exist.", model.RoleName);
                return NotFound("Role not found.");
            }

            var result = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (result.Succeeded)
            {
                _logger.LogInformation("Role '{RoleName}' assigned to user '{UserId}' successfully.", model.RoleName, model.UserId);
                return Ok("Role assigned to user successfully.");
            }

            _logger.LogError("Error occurred while assigning role '{RoleName}' to user '{UserId}': {Errors}", model.RoleName, model.UserId, result.Errors);
            return BadRequest(result.Errors);
        }
    }
}
