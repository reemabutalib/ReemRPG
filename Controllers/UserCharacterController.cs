using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReemRPG.Data;
using ReemRPG.Models;

namespace ReemRPG.Controllers
{
    public class SelectCharacterRequest
    {
        public int CharacterId { get; set; }
    }
    [Route("api/[controller]")]
    [ApiController]
    public class UserCharacterController : ControllerBase // Change to ControllerBase for API
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<UserCharacterController> _logger;

        public UserCharacterController(ApplicationContext context, ILogger<UserCharacterController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Get current user ID
        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // GET: api/UserCharacter - Get all user characters (admin only)
        [HttpGet]
        public async Task<IActionResult> GetUserCharacters()
        {
            var userCharacters = await _context.UserCharacters
                .Include(u => u.Character)
                .Select(uc => new
                {
                    userId = uc.UserId,
                    characterId = uc.CharacterId,
                    characterName = uc.Character.Name,
                    class_ = uc.Character.Class,
                    level = uc.Level,
                    experience = uc.Experience,
                    gold = uc.Gold
                })
                .ToListAsync();

            return Ok(userCharacters);
        }

        // GET: api/UserCharacter/selected - Get the user's selected character
        [HttpGet("selected")]
        [Authorize]
        public async Task<IActionResult> GetSelectedCharacter()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Try to get the selected character first
                var userCharacter = await _context.UserCharacters
                    .Include(u => u.Character)
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.IsSelected);

                // If no selected character, get the first one if it exists
                if (userCharacter == null)
                {
                    userCharacter = await _context.UserCharacters
                        .Include(u => u.Character)
                        .FirstOrDefaultAsync(uc => uc.UserId == userId);

                    // If found, mark it as selected
                    if (userCharacter != null)
                    {
                        userCharacter.IsSelected = true;
                        await _context.SaveChangesAsync();
                    }
                }

                if (userCharacter == null)
                {
                    return NotFound(new { message = "No character found for this user" });
                }

                // Return character data
                return Ok(new
                {
                    characterId = userCharacter.CharacterId,
                    name = userCharacter.Character.Name,
                    class_ = userCharacter.Character.Class,
                    level = userCharacter.Level,
                    experience = userCharacter.Experience,
                    gold = userCharacter.Gold,
                    imageUrl = userCharacter.Character.ImageUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting selected character");
                return StatusCode(500, new { message = "Error retrieving character" });
            }
        }

        // Add this method inside the UserCharacterController class, at the end before the closing }

        // Add this endpoint to check database connectivity
        [HttpGet("test-db")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var characterCount = await _context.Characters.CountAsync();
                var userCount = await _context.Users.CountAsync();

                return Ok(new
                {
                    message = "Database connection successful",
                    characterCount = characterCount,
                    userCount = userCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Database error",
                    error = ex.Message
                });
            }
        }

        // POST: api/UserCharacter/select - Select a character
        [HttpPost("select")]
        [Authorize]
        public async Task<IActionResult> SelectCharacter([FromBody] SelectCharacterRequest request)
        {
            try
            {
                if (request == null || request.CharacterId <= 0)
                {
                    return BadRequest(new { message = "Invalid character ID" });
                }

                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Verify the user exists in the database
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    _logger.LogWarning("User ID {UserId} not found in database", userId);
                    return NotFound(new { message = "User not found in database" });
                }

                // Find the character with explicit logging
                var character = await _context.Characters.FindAsync(request.CharacterId);
                if (character == null)
                {
                    _logger.LogWarning("Character with ID {CharacterId} not found in database", request.CharacterId);
                    return NotFound(new { message = "Character not found" });
                }

                _logger.LogInformation("Processing character selection: User {UserId}, Character {CharacterId}", userId, request.CharacterId);

                // Start a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Check if user already has this character
                    var existingUserCharacter = await _context.UserCharacters
                        .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == request.CharacterId);

                    if (existingUserCharacter == null)
                    {
                        // Log detailed info
                        _logger.LogInformation("Creating new user-character association with CharacterId={CharacterId} and UserId={UserId}",
                            request.CharacterId, userId);

                        // Create new association
                        existingUserCharacter = new UserCharacter
                        {
                            UserId = userId,
                            CharacterId = request.CharacterId,
                            Level = 1,
                            Experience = 0,
                            Gold = 0,
                            IsSelected = true
                        };
                        _context.UserCharacters.Add(existingUserCharacter);
                    }
                    else
                    {
                        existingUserCharacter.IsSelected = true;
                    }

                    // Deselect other characters
                    var otherCharacters = await _context.UserCharacters
                        .Where(uc => uc.UserId == userId && uc.CharacterId != request.CharacterId)
                        .ToListAsync();

                    foreach (var other in otherCharacters)
                    {
                        other.IsSelected = false;
                    }

                    // Save changes
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        characterId = existingUserCharacter.CharacterId,
                        name = character.Name,
                        class_ = character.Class,
                        level = existingUserCharacter.Level,
                        experience = existingUserCharacter.Experience,
                        gold = existingUserCharacter.Gold,
                        imageUrl = character.ImageUrl ?? ""
                    });
                }
                catch (Exception innerEx)
                {
                    // Rollback on error
                    await transaction.RollbackAsync();

                    _logger.LogError(innerEx,
                        "Database error while processing character selection. User: {UserId}, Character: {CharacterId}",
                        userId, request.CharacterId);

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting character: {Message}", ex.Message);

                return StatusCode(500, new
                {
                    message = "Error selecting character",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }

        }
    }
}
