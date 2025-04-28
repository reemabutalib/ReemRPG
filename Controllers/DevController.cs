using Microsoft.EntityFrameworkCore;
using ReemRPG.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReemRPG.Data;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Security.Claims;

namespace ReemRPG.Controllers
{
    public class ResetRequest
    {
        public string Token { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class DevController : ControllerBase
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<DevController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public DevController(
            ApplicationContext context,
            ILogger<DevController> logger,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _env = env;
            _configuration = configuration;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "DevController is alive", timestamp = DateTime.UtcNow });
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetDatabase([FromBody] ResetRequest request)
        {
            // Only allow in Development environment
            if (!_env.IsDevelopment())
            {
                _logger.LogWarning("Attempted to access dev reset endpoint in non-development environment");
                return BadRequest("This endpoint is only available in development environment");
            }

            // Add an extra security check using a dev password from configuration
            string devPassword = _configuration["DevSettings:ResetPassword"];
            if (string.IsNullOrEmpty(devPassword))
            {
                _logger.LogWarning("DevSettings:ResetPassword not configured in appsettings");
                return BadRequest("Reset password not configured");
            }

            // Check the token from the request
            if (request == null || request.Token != devPassword)
            {
                _logger.LogWarning("Invalid reset password provided");
                return Unauthorized("Invalid reset password");
            }

            try
            {
                _logger.LogInformation("Starting database reset process");

                // First, try to delete all character quests
                try
                {
                    var characterQuests = await _context.CharacterQuests.ToListAsync();
                    if (characterQuests.Any())
                    {
                        _context.CharacterQuests.RemoveRange(characterQuests);
                        _logger.LogInformation("Removed all character quests");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error removing character quests");
                }

                // Try to delete all quest completions if the table exists
                try
                {
                    // Use dynamic to avoid compile-time checking
                    dynamic dbContext = _context;
                    var questCompletions = await dbContext.QuestCompletions.ToListAsync();
                    if (questCompletions.Count > 0)
                    {
                        dbContext.QuestCompletions.RemoveRange(questCompletions);
                        _logger.LogInformation("Removed all quest completions");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "QuestCompletions table not accessible or doesn't exist");
                }

                // Delete user-character associations
                try
                {
                    var userCharacters = await _context.UserCharacters.ToListAsync();
                    if (userCharacters.Any())
                    {
                        _context.UserCharacters.RemoveRange(userCharacters);
                        _logger.LogInformation("Removed all user-character associations");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error removing user characters");
                }

                // Delete characters
                try
                {
                    var characters = await _context.Characters.ToListAsync();
                    if (characters.Any())
                    {
                        _context.Characters.RemoveRange(characters);
                        _logger.LogInformation("Removed all characters");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error removing characters");
                }

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Database reset successful");
                return Ok(new { message = "Database has been reset" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting database");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}