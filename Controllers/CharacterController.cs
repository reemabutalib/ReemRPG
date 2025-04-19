using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ReemRPG.Models;
using ReemRPG.Services.Interfaces;

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CharacterController : ControllerBase
    {
        private readonly ICharacterService _characterService;
        private readonly ILogger<CharacterController> _logger;

        public CharacterController(ICharacterService characterService, ILogger<CharacterController> logger)
        {
            _characterService = characterService;
            _logger = logger;
        }

        // GET: api/Character
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Character>>> GetCharacters()
        {
            _logger.LogInformation("Fetching all characters");
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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Character>> PostCharacter(Character character)
        {
            _logger.LogInformation("Creating a new character");
            var createdCharacter = await _characterService.CreateCharacterAsync(character);
            return CreatedAtAction(nameof(GetCharacter), new { id = createdCharacter.CharacterId }, createdCharacter);
        }

        // GET: api/Character/leaderboard
        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            _logger.LogInformation("Fetching leaderboard data.");
            var leaderboard = await _characterService.GetLeaderboardAsync();
            return Ok(leaderboard);
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
            if (request == null || request.CharacterId == 0)
            {
                _logger.LogWarning("CharacterId is required for selecting a character.");
                return BadRequest(new { message = "CharacterId is required." });
            }

            // Retrieve userId from the token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                _logger.LogWarning("User ID is not present in the token.");
                return Unauthorized(new { message = "User not authenticated." });
            }

            string userId = userIdClaim.Value; // Extract userId as a string

            _logger.LogInformation($"User {userId} selecting character with ID: {request.CharacterId}");
            var success = await _characterService.SelectCharacterAsync(userId, request.CharacterId);
            if (!success)
            {
                _logger.LogWarning($"Character with ID {request.CharacterId} could not be selected by user {userId}");
                return NotFound(new { message = "Character not found or cannot be selected." });
            }

            return Ok(new { message = "Character selected successfully." });
        }

        public class SelectCharacterRequest
        {
            public int CharacterId { get; set; }
        }
    }
}