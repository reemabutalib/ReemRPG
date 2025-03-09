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
        private readonly ILogger<CharacterController> _logger; // ILogger: used for debugging and tracking application events. Logs help identify issues, record user actions, and monitor application health.

        public CharacterController(ICharacterService characterService, ILogger<CharacterController> logger)
        {
            _characterService = characterService;
            _logger = logger; // Assign logger
        }

        // GET: api/Character
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Character>>> GetCharacters()
        {
            _logger.LogInformation("Fetching all characters"); // log entry created every time method is called, helps track API usage and debugging
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
    }
}
