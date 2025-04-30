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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Debugging log to see what's returned
            _logger.LogInformation("GetUserId method returned: {UserId}", userId);

            return userId;
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
                _logger.LogInformation("GetSelectedCharacter endpoint called");

                var claimValue = GetUserId(); // This returns the email based on your logs
                if (claimValue == null)
                {
                    _logger.LogWarning("GetSelectedCharacter: User not authenticated");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Look up user by either ID or email (just like in SelectCharacter)
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Id == claimValue || u.Email == claimValue);

                if (user == null)
                {
                    _logger.LogWarning("GetSelectedCharacter: User with identifier {Identifier} not found in database", claimValue);
                    return NotFound(new { message = "User not found in database" });
                }

                // Use the actual user ID going forward
                var userId = user.Id;
                _logger.LogInformation("GetSelectedCharacter: Found user with identifier {Identifier}, using ID: {UserId}",
                    claimValue, userId);

                // Try to get the selected character with explicit error handling
                try
                {
                    // Try to get the selected character first
                    var userCharacter = await _context.UserCharacters
                        .Include(u => u.Character)
                        .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.IsSelected);

                    _logger.LogInformation("GetSelectedCharacter: Initial query for selected character returned: {Result}",
                        userCharacter != null ? "found" : "not found");

                    // If no selected character, get the first one if it exists
                    if (userCharacter == null)
                    {
                        userCharacter = await _context.UserCharacters
                            .Include(u => u.Character)
                            .FirstOrDefaultAsync(uc => uc.UserId == userId);

                        _logger.LogInformation("GetSelectedCharacter: Fallback query for any character returned: {Result}",
                            userCharacter != null ? "found" : "not found");

                        // If found, mark it as selected
                        if (userCharacter != null)
                        {
                            userCharacter.IsSelected = true;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("GetSelectedCharacter: Marked character {CharacterId} as selected",
                                userCharacter.CharacterId);
                        }
                    }

                    if (userCharacter == null)
                    {
                        _logger.LogInformation("GetSelectedCharacter: No character found for user {UserId}", userId);
                        return NotFound(new { message = "No character found for this user" });
                    }

                    // Return character data
                    var result = new
                    {
                        characterId = userCharacter.CharacterId,
                        name = userCharacter.Character.Name,
                        class_ = userCharacter.Character.Class,
                        level = userCharacter.Level,
                        experience = userCharacter.Experience,
                        gold = userCharacter.Gold,
                        imageUrl = userCharacter.Character.ImageUrl ?? ""
                    };

                    _logger.LogInformation("GetSelectedCharacter: Successfully retrieved character {CharacterId} for user {UserId}",
                        userCharacter.CharacterId, userId);

                    return Ok(result);
                }
                catch (InvalidOperationException invEx)
                {
                    // This can happen if there's a DB schema mismatch (e.g., missing Id column)
                    _logger.LogError(invEx, "GetSelectedCharacter: Invalid operation in database query");

                    return StatusCode(500, new
                    {
                        message = "Database schema error",
                        error = invEx.Message
                    });
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "GetSelectedCharacter: Database error when querying for selected character");

                    return StatusCode(500, new
                    {
                        message = "Database error",
                        error = dbEx.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSelectedCharacter: Unhandled exception");
                return StatusCode(500, new { message = "Error retrieving character", error = ex.Message });
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

                var claimValue = GetUserId(); // This returns the email based on your logs
                if (claimValue == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Look up user by either ID or email
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Id == claimValue || u.Email == claimValue);

                if (user == null)
                {
                    _logger.LogWarning("User with identifier {Identifier} not found in database", claimValue);
                    return NotFound(new { message = "User not found in database" });
                }

                // Use the actual user ID going forward
                var userId = user.Id;
                _logger.LogInformation("Found user with email {Email}, using ID: {UserId}", claimValue, userId);

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
