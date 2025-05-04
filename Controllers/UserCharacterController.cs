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
    [Route("api/usercharacter")]
    [ApiController]
    public class UserCharacterController : ControllerBase
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

        // GET: api/UserCharacter - Get the current user's characters
        [HttpGet]
        [Authorize] // Make sure this endpoint is authorized
        public async Task<IActionResult> GetUserCharacters()
        {
            try
            {
                var claimValue = GetUserId();
                if (claimValue == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Look up user by either ID or email
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Id == claimValue || u.Email == claimValue);

                if (user == null)
                {
                    _logger.LogWarning("GetUserCharacters: User with identifier {Identifier} not found in database", claimValue);
                    return NotFound(new { message = "User not found" });
                }

                // Use the actual user ID going forward
                var userId = user.Id;
                _logger.LogInformation("GetUserCharacters: Found user with identifier {Identifier}, using ID: {UserId}",
                    claimValue, userId);

                // Query UserCharacters with proper filtering by current user
                var userCharacters = await _context.UserCharacters
                    .Where(uc => uc.UserId == userId) // CRITICAL: Filter by the current user's ID
                    .Include(uc => uc.Character)
                    .Select(uc => new
                    {
                        userCharacterId = uc.Id, // This should be the primary key
                        userId = uc.UserId,
                        characterId = uc.CharacterId,
                        characterName = uc.Character.Name,
                        class_ = uc.Character.Class,
                        level = uc.Level,
                        experience = uc.Experience,
                        gold = uc.Gold,
                        isSelected = uc.IsSelected // Include isSelected flag
                    })
                    .ToListAsync();

                // Log for debugging
                _logger.LogInformation("GetUserCharacters: Retrieved {Count} characters for user {UserId}",
                    userCharacters.Count, userId);

                return Ok(userCharacters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserCharacters: Error retrieving user characters: {Message}", ex.Message);

                return StatusCode(500, new
                {
                    message = "Error retrieving characters",
                    error = ex.Message
                });
            }
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
                        userCharacterId = userCharacter.Id,
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
                // Check for either characterId or userCharacterId
                if (request == null ||
                    (request.CharacterId <= 0 && request.UserCharacterId <= 0))
                {
                    return BadRequest(new { message = "Invalid character ID" });
                }

                var claimValue = GetUserId();
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

                // Find the user character - look up by userCharacterId first if provided
                UserCharacter existingUserCharacter = null;

                if (request.UserCharacterId > 0)
                {
                    // Find by userCharacterId directly
                    existingUserCharacter = await _context.UserCharacters
                        .Include(uc => uc.Character)
                        .FirstOrDefaultAsync(uc =>
                            uc.UserId == userId &&
                            uc.Id == request.UserCharacterId);

                    if (existingUserCharacter == null)
                    {
                        _logger.LogWarning("UserCharacter with ID {UserCharacterId} not found for user {UserId}",
                            request.UserCharacterId, userId);
                        return NotFound(new { message = "Character not found or doesn't belong to you" });
                    }
                }
                else
                {
                    // Fall back to characterId if userCharacterId not provided
                    // Find the character
                    var character = await _context.Characters.FindAsync(request.CharacterId);
                    if (character == null)
                    {
                        _logger.LogWarning("Character with ID {CharacterId} not found in database", request.CharacterId);
                        return NotFound(new { message = "Character not found" });
                    }

                    _logger.LogInformation("Processing character selection: User {UserId}, Character {CharacterId}",
                        userId, request.CharacterId);

                    // Check if user already has this character
                    existingUserCharacter = await _context.UserCharacters
                        .Include(uc => uc.Character)
                        .FirstOrDefaultAsync(uc =>
                            uc.UserId == userId &&
                            uc.CharacterId == request.CharacterId);

                    // Create new user-character association if it doesn't exist
                    if (existingUserCharacter == null)
                    {
                        // Log detailed info
                        _logger.LogInformation("Creating new user-character association with CharacterId={CharacterId} and UserId={UserId}",
                            request.CharacterId, userId);

                        // Create new association
                        existingUserCharacter = new UserCharacter
                        {
                            UserId = userId,
                            CharacterId = request.CharacterId.Value,
                            Level = 1,
                            Experience = 0,
                            Gold = 0,
                            IsSelected = true
                        };
                        _context.UserCharacters.Add(existingUserCharacter);

                        // Need to save immediately to get the ID
                        await _context.SaveChangesAsync();

                        // Reload to get Character navigation property
                        existingUserCharacter = await _context.UserCharacters
                            .Include(uc => uc.Character)
                            .FirstOrDefaultAsync(uc =>
                                uc.Id == existingUserCharacter.Id);
                    }
                }

                // Start a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Mark this character as selected
                    existingUserCharacter.IsSelected = true;

                    // Deselect other characters
                    var otherCharacters = await _context.UserCharacters
                        .Where(uc =>
                            uc.UserId == userId &&
                            uc.Id != existingUserCharacter.Id) // Compare by primary key ID
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
                        userCharacterId = existingUserCharacter.Id, // Include userCharacterId in response
                        name = existingUserCharacter.Character.Name,
                        class_ = existingUserCharacter.Character.Class,
                        level = existingUserCharacter.Level,
                        experience = existingUserCharacter.Experience,
                        gold = existingUserCharacter.Gold,
                        imageUrl = existingUserCharacter.Character.ImageUrl ?? ""
                    });
                }
                catch (Exception innerEx)
                {
                    // Rollback on error
                    await transaction.RollbackAsync();

                    _logger.LogError(innerEx,
                        "Database error while processing character selection. User: {UserId}, Character: {CharacterId}",
                        userId, existingUserCharacter?.CharacterId);

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

        // POST: api/UserCharacter/create - Create a user character from a base character
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateUserCharacter([FromBody] SelectCharacterRequest request)
        {
            try
            {
                if (request == null || request.CharacterId <= 0)
                {
                    return BadRequest(new { message = "Invalid character ID" });
                }

                var claimValue = GetUserId();
                if (claimValue == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Look up user by either ID or email
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Id == claimValue || u.Email == claimValue);

                if (user == null)
                {
                    _logger.LogWarning("CreateUserCharacter: User with identifier {Identifier} not found in database", claimValue);
                    return NotFound(new { message = "User not found in database" });
                }

                // Use the actual user ID going forward
                var userId = user.Id;
                _logger.LogInformation("CreateUserCharacter: Found user with identifier {Identifier}, using ID: {UserId}",
                    claimValue, userId);

                // Find the base character
                var character = await _context.Characters.FindAsync(request.CharacterId);
                if (character == null)
                {
                    _logger.LogWarning("CreateUserCharacter: Character with ID {CharacterId} not found in database", request.CharacterId);
                    return NotFound(new { message = "Character not found" });
                }

                _logger.LogInformation("CreateUserCharacter: Processing creation for User {UserId}, Character {CharacterId}",
                    userId, request.CharacterId);

                // Check if user already has this character
                var existingUserCharacter = await _context.UserCharacters
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == request.CharacterId);

                if (existingUserCharacter != null)
                {
                    _logger.LogWarning("CreateUserCharacter: User {UserId} already has character {CharacterId}",
                        userId, request.CharacterId);

                    return Conflict(new
                    {
                        message = "You already have this character in your collection",
                        characterId = existingUserCharacter.CharacterId
                    });
                }

                // Start a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create new user character
                    var userCharacter = new UserCharacter
                    {
                        UserId = userId,
                        CharacterId = request.CharacterId.Value,
                        Level = 1,
                        Experience = 0,
                        Gold = 100, // Give some starting gold
                        IsSelected = false // Don't auto-select
                    };

                    _context.UserCharacters.Add(userCharacter);
                    await _context.SaveChangesAsync();

                    // If this is the user's first character, mark it as selected
                    var characterCount = await _context.UserCharacters
                        .CountAsync(uc => uc.UserId == userId);

                    if (characterCount == 1)
                    {
                        userCharacter.IsSelected = true;
                        await _context.SaveChangesAsync();
                    }

                    // Commit transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("CreateUserCharacter: Successfully created character for User {UserId}, Character {CharacterId}, Selected: {IsSelected}",
                        userId, request.CharacterId, characterCount == 1);

                    // Return the character data
                    return Ok(new
                    {
                        characterId = userCharacter.CharacterId,
                        userCharacterId = userCharacter.Id,
                        name = character.Name,
                        class_ = character.Class,
                        level = userCharacter.Level,
                        experience = userCharacter.Experience,
                        gold = userCharacter.Gold,
                        imageUrl = character.ImageUrl ?? "",
                        isSelected = userCharacter.IsSelected
                    });
                }
                catch (Exception innerEx)
                {
                    // Rollback on error
                    await transaction.RollbackAsync();

                    _logger.LogError(innerEx,
                        "CreateUserCharacter: Database error while creating user character. User: {UserId}, Character: {CharacterId}",
                        userId, request.CharacterId);

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateUserCharacter: Error creating user character: {Message}", ex.Message);

                return StatusCode(500, new
                {
                    message = "Error creating character",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // DELETE: api/usercharacter/{userCharacterId} - Remove a character from user's collection
        [HttpDelete("{userCharacterId}")]
        [Authorize]
        public async Task<IActionResult> DeleteUserCharacter(int userCharacterId)
        {
            try
            {
                if (userCharacterId <= 0)
                {
                    return BadRequest(new { message = "Invalid character ID" });
                }

                var claimValue = GetUserId();
                if (claimValue == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Look up user by either ID or email
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Id == claimValue || u.Email == claimValue);

                if (user == null)
                {
                    _logger.LogWarning("DeleteUserCharacter: User with identifier {Identifier} not found in database", claimValue);
                    return NotFound(new { message = "User not found in database" });
                }

                // Use the actual user ID going forward
                var userId = user.Id;

                // Find the user character by the UserCharacterId (which is the Id field)
                var userCharacter = await _context.UserCharacters
                    .FirstOrDefaultAsync(uc =>
                        uc.UserId == userId &&
                        uc.Id == userCharacterId); // FIXED: Use Id instead of CharacterId

                if (userCharacter == null)
                {
                    return NotFound(new { message = "Character not found or doesn't belong to you" });
                }

                // Check if it was the selected character
                bool wasSelected = userCharacter.IsSelected;

                // Start transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Remove the user character
                    _context.UserCharacters.Remove(userCharacter);
                    await _context.SaveChangesAsync();

                    // If this was the selected character, try to select another one
                    if (wasSelected)
                    {
                        var nextCharacter = await _context.UserCharacters
                            .FirstOrDefaultAsync(uc => uc.UserId == userId);

                        if (nextCharacter != null)
                        {
                            nextCharacter.IsSelected = true;
                            await _context.SaveChangesAsync();
                        }
                    }

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        message = "Character removed successfully",
                        wasSelected = wasSelected
                    });
                }
                catch (Exception innerEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(innerEx, "DeleteUserCharacter: Error deleting character {UserCharacterId} for user {UserId}",
                        userCharacterId, userId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteUserCharacter: Error deleting character: {Message}", ex.Message);

                return StatusCode(500, new
                {
                    message = "Error removing character",
                    error = ex.Message
                });
            }
        }
    }
}