using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ReemRPG.Data;
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
        private readonly ApplicationContext _context;

        public CharacterController(ICharacterService characterService, ILogger<CharacterController> logger, ApplicationContext context)
        {
            _characterService = characterService;
            _logger = logger;
            _context = context;
        }

        // GET: api/Character - Get all base characters
        [HttpGet]
        public async Task<IActionResult> GetCharacters()
        {
            try
            {
                var characters = await _characterService.GetAllCharactersAsync();

                // Convert to frontend-friendly format with consistent property naming
                var result = characters.Select(c => new
                {
                    characterId = c.CharacterId,
                    name = c.Name,
                    class_ = c.Class,
                    imageUrl = c.ImageUrl,
                    baseStrength = c.BaseStrength,
                    baseAgility = c.BaseAgility,
                    baseIntelligence = c.BaseIntelligence,
                    baseHealth = c.BaseHealth
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching characters");
                return StatusCode(500, new { message = "Error retrieving characters" });
            }
        }

        // GET: api/Character/5 - Get a specific base character
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCharacter(int id)
        {
            try
            {
                var character = await _characterService.GetCharacterByIdAsync(id);

                if (character == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                // Return with consistent property names
                var result = new
                {
                    characterId = character.CharacterId,
                    name = character.Name,
                    class_ = character.Class,
                    imageUrl = character.ImageUrl,
                    baseStrength = character.BaseStrength,
                    baseAgility = character.BaseAgility,
                    baseIntelligence = character.BaseIntelligence,
                    baseHealth = character.BaseHealth
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching character {Id}", id);
                return StatusCode(500, new { message = "Error retrieving character" });
            }
        }

        // ADMIN ENDPOINTS

        // POST: api/Character - Create a new base character (admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCharacter(Character character)
        {
            try
            {
                if (character == null)
                {
                    return BadRequest(new { message = "Invalid character data" });
                }

                var createdCharacter = await _characterService.CreateCharacterAsync(character);

                return CreatedAtAction(
                    nameof(GetCharacter),
                    new { id = createdCharacter.CharacterId },
                    new
                    {
                        characterId = createdCharacter.CharacterId,
                        name = createdCharacter.Name,
                        class_ = createdCharacter.Class
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating character");
                return StatusCode(500, new { message = "Error creating character" });
            }
        }

        // PUT: api/Character/5 - Update a base character (admin only)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCharacter(int id, Character character)
        {
            try
            {
                if (id != character.CharacterId)
                {
                    return BadRequest(new { message = "ID mismatch" });
                }

                var updatedCharacter = await _characterService.UpdateCharacterAsync(id, character);
                if (updatedCharacter == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating character {Id}", id);
                return StatusCode(500, new { message = "Error updating character" });
            }
        }

        // DELETE: api/Character/5 - Delete a base character (admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCharacter(int id)
        {
            try
            {
                bool deleted = await _characterService.DeleteCharacterAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = "Character not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting character {Id}", id);
                return StatusCode(500, new { message = "Error deleting character" });
            }
        }
    }
}