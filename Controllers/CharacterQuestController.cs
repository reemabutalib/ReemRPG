using ReemRPG.Data;
using ReemRPG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Added for logging

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CharacterQuestController : ControllerBase
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<CharacterQuestController> _logger; // Inject ILogger

        public CharacterQuestController(ApplicationContext context, ILogger<CharacterQuestController> logger)
        {
            _context = context;
            _logger = logger; // Assign logger
        }

        // GET: api/CharacterQuest
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CharacterQuest>>> GetCharacterQuests()
        {
            _logger.LogInformation("Retrieving all character quests.");

            var quests = await _context.CharacterQuests.ToListAsync();
            return Ok(quests);
        }

        // GET: api/CharacterQuest/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CharacterQuest>> GetCharacterQuest(int id)
        {
            _logger.LogInformation("Fetching CharacterQuest with ID: {Id}", id);

            var characterQuest = await _context.CharacterQuests.FindAsync(id);
            if (characterQuest == null)
            {
                _logger.LogWarning("CharacterQuest with ID {Id} not found.", id);
                return NotFound();
            }

            return Ok(characterQuest);
        }

        // PUT: api/CharacterQuest/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCharacterQuest(int id, CharacterQuest characterQuest)
        {
            if (id != characterQuest.Id)
            {
                _logger.LogWarning("PUT request failed. Mismatched IDs: {Id} != {CharacterQuestId}", id, characterQuest.Id);
                return BadRequest();
            }

            _context.Entry(characterQuest).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("CharacterQuest with ID {Id} updated successfully.", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CharacterQuestExists(id))
                {
                    _logger.LogWarning("CharacterQuest update failed. ID {Id} does not exist.", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error while updating CharacterQuest with ID {Id}.", id);
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/CharacterQuest
        [HttpPost]
        public async Task<ActionResult<CharacterQuest>> PostCharacterQuest(CharacterQuest characterQuest)
        {
            _logger.LogInformation("Creating a new CharacterQuest.");

            _context.CharacterQuests.Add(characterQuest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("CharacterQuest with ID {Id} created successfully.", characterQuest.Id);
            return CreatedAtAction("GetCharacterQuest", new { id = characterQuest.Id }, characterQuest);
        }

        // DELETE: api/CharacterQuest/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCharacterQuest(int id)
        {
            _logger.LogInformation("Attempting to delete CharacterQuest with ID {Id}.", id);

            var characterQuest = await _context.CharacterQuests.FindAsync(id);
            if (characterQuest == null)
            {
                _logger.LogWarning("Deletion failed. CharacterQuest with ID {Id} not found.", id);
                return NotFound();
            }

            _context.CharacterQuests.Remove(characterQuest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("CharacterQuest with ID {Id} deleted successfully.", id);
            return NoContent();
        }

        private bool CharacterQuestExists(int id)
        {
            return _context.CharacterQuests.Any(e => e.Id == id);
        }
    }
}
