using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ReemRPG.Models;
using ReemRPG.Services.Interfaces;
using ReemRPG.Data;
using ReemRPG.DTOs;

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CharacterController : ControllerBase
    {
        private readonly ICharacterService _characterService;
        private readonly ILogger<CharacterController> _logger;
        private readonly ApplicationContext _context;

        public CharacterController(ICharacterService characterService, ILogger<CharacterController> logger, ApplicationContext context)
        {
            _characterService = characterService;
            _logger = logger;
            _context = context; // Assign context for debugging
        }

        // GET: api/Character
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Character>>> GetCharacters()
        {
            _logger.LogInformation("Fetching all characters");

            // Get all characters without filtering by user
            var characters = await _characterService.GetAllCharactersAsync();
            return Ok(characters);
        }

        // GET: api/Character/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Character>> GetCharacter(int id)
        {
            _logger.LogInformation($"Fetching character with ID: {id}");
            var character = await _characterService.GetCharacterByIdAsync(id);
            if (character == null)
            {
                _logger.LogWarning($"Character with ID {id} not found");
                return NotFound();
            }
            return Ok(character);
        }

        // PUT: api/Character/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCharacter(int id, Character character)
        {
            _logger.LogInformation($"Updating character with ID: {id}");
            var updatedCharacter = await _characterService.UpdateCharacterAsync(id, character);
            if (updatedCharacter == null)
            {
                _logger.LogWarning($"Character with ID {id} not found for update");
                return NotFound();
            }
            return NoContent();
        }

        // POST: api/Character
        [HttpPost]
        public async Task<ActionResult<Character>> PostCharacter(Character character)
        {
            _logger.LogInformation("Creating a new character");
            var createdCharacter = await _characterService.CreateCharacterAsync(character);
            return CreatedAtAction(nameof(GetCharacter), new { id = createdCharacter.CharacterId }, createdCharacter);
        }

        // DELETE: api/Character/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCharacter(int id)
        {
            _logger.LogInformation($"Deleting character with ID: {id}");
            var deleted = await _characterService.DeleteCharacterAsync(id);
            if (!deleted)
            {
                _logger.LogWarning($"Character with ID {id} not found for deletion");
                return NotFound();
            }
            return NoContent();
        }

        // POST: api/Character/select-character
        [HttpPost("select-character")]
        [Authorize]
        public async Task<IActionResult> SelectCharacter([FromBody] SelectCharacterRequest request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Invalid request for selecting a character.");
                    return BadRequest(new { message = "Invalid request." });
                }

                // Check for CharacterId vs. characterId (casing differences)
                int characterId = request.CharacterId;

                // If CharacterId is 0, check if we received it in different casing
                if (characterId == 0 && request.GetType().GetProperty("characterId") != null)
                {
                    characterId = (int)request.GetType().GetProperty("characterId").GetValue(request, null);
                }

                if (characterId == 0)
                {
                    _logger.LogWarning("CharacterId is required for selecting a character.");
                    return BadRequest(new { message = "CharacterId is required." });
                }

                // Retrieve the userId from the claim
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                {
                    _logger.LogWarning("User ID is not present in the token.");
                    return Unauthorized(new { message = "User not authenticated." });
                }

                string userId = userIdClaim.Value;
                _logger.LogInformation($"User {userId} selecting character with ID: {characterId}");

                // Check if the character exists in base characters
                var character = await _context.Characters
                    .AsNoTracking() // Avoid tracking issues
                    .FirstOrDefaultAsync(c => c.CharacterId == characterId);

                if (character == null)
                {
                    _logger.LogWarning($"Character with ID {characterId} not found");
                    return NotFound(new { message = "Character not found." });
                }

                // Check if user already has this character
                var existingUserCharacter = await _context.UserCharacters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

                // If the user doesn't have this character yet, create the association
                if (existingUserCharacter == null)
                {
                    try
                    {
                        _logger.LogInformation($"Creating new UserCharacter for user {userId}, character {characterId}");

                        var newUserCharacter = new UserCharacter
                        {
                            UserId = userId,
                            CharacterId = characterId,
                            Level = 1,
                            Experience = 0,
                            Gold = 0
                        };

                        await _context.UserCharacters.AddAsync(newUserCharacter);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Successfully created UserCharacter for user {userId}, character {characterId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error creating user character association: {ex.Message}");
                        return StatusCode(500, new { message = "Error associating character with user", error = ex.Message });
                    }
                }
                else
                {
                    _logger.LogInformation($"User {userId} already has character {characterId}");
                }

                // Get the character data with progression info
                var userCharacter = await _context.UserCharacters
                    .AsNoTracking() // Avoid tracking issues
                    .Include(uc => uc.Character)
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

                if (userCharacter == null)
                {
                    _logger.LogError($"Failed to retrieve UserCharacter after creation for user {userId}, character {characterId}");
                    return StatusCode(500, new { message = "Character association failed" });
                }

                // Return character with user's progression data
                var characterDto = new CharacterDTO
                {
                    CharacterId = userCharacter.Character.CharacterId,
                    Name = userCharacter.Character.Name,
                    Class = userCharacter.Character.Class,
                    ImageUrl = userCharacter.Character.ImageUrl,
                    BaseStrength = userCharacter.Character.BaseStrength,
                    BaseAgility = userCharacter.Character.BaseAgility,
                    BaseIntelligence = userCharacter.Character.BaseIntelligence,
                    BaseHealth = userCharacter.Character.BaseHealth,
                    BaseAttackPower = userCharacter.Character.BaseAttackPower,
                    Level = userCharacter.Level,
                    Experience = userCharacter.Experience,
                    Gold = userCharacter.Gold,
                    Health = userCharacter.Character.BaseHealth + (userCharacter.Level * 10),
                    AttackPower = userCharacter.Character.BaseAttackPower + (userCharacter.Level * 2),
                    IsAssociatedWithUser = true
                };

                return Ok(new { message = "Character selected successfully", character = characterDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error selecting character: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while processing your request", error = ex.Message });
            }
        }

        // GET: api/Character/get-selected-character
        [HttpGet("get-selected-character")]
        [Authorize]
        public async Task<IActionResult> GetSelectedCharacter()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                _logger.LogWarning("User ID (jti) is not present in the token.");
                return Unauthorized(new { message = "User not authenticated." });
            }

            string userId = userIdClaim.Value;
            _logger.LogInformation($"Fetching selected character for user {userId}");

            var selectedCharacter = await _characterService.GetSelectedCharacterAsync(userId);
            if (selectedCharacter == null)
            {
                _logger.LogWarning($"No character selected for user {userId}");
                return NotFound(new { message = "No character selected." });
            }

            _logger.LogInformation($"Character found: {selectedCharacter.Name}");
            return Ok(selectedCharacter);
        }

        // GET: api/Character/{id}/validate
        [HttpGet("{id}/validate")]
        [Authorize]
        public async Task<IActionResult> ValidateOwnership(int id)
        {
            // Get the current user's ID from the token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                _logger.LogWarning("User ID is not present in the token.");
                return Unauthorized(new { message = "User not authenticated." });
            }

            string userId = userIdClaim.Value;
            _logger.LogInformation($"Validating ownership of character {id} for user {userId}");

            // Try to find the character
            var character = await _context.Characters.FindAsync(id);
            if (character == null)
            {
                _logger.LogWarning($"Character with ID {id} not found");
                return NotFound(new { message = "Character not found.", isOwner = false });
            }

            // Check if this user is associated with this character in the UserCharacters table
            var userCharacter = await _context.UserCharacters
                .FirstOrDefaultAsync(uc => uc.CharacterId == id && uc.UserId == userId);

            bool isOwner = userCharacter != null;
            _logger.LogInformation($"Character ownership validation result: {isOwner}");

            return Ok(new { isOwner = isOwner });
        }

        [HttpGet("debug/characters")]
        public async Task<IActionResult> DebugCharacters()
        {
            var characters = await _context.Characters.ToListAsync();
            return Ok(new { Count = characters.Count, Characters = characters });
        }

        [HttpGet("debug/usercharacters")]
        public async Task<IActionResult> DebugUserCharacters()
        {
            var userCharacters = await _context.UserCharacters.ToListAsync();
            return Ok(new { Count = userCharacters.Count, UserCharacters = userCharacters });
        }

        public class SelectCharacterRequest
        {
            public int CharacterId { get; set; }
        }
    }
}