using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; 

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestController : ControllerBase
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<QuestController> _logger; // Inject ILogger

        public QuestController(ApplicationContext context, ILogger<QuestController> logger)
        {
            _context = context;
            _logger = logger; // Assign logger
        }

        // GET: api/Quest
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Quest>>> GetQuests()
        {
            _logger.LogInformation("Fetching all quests.");
            var quests = await _context.Quests.ToListAsync();
            return Ok(quests);
        }

        // GET: api/Quest/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Quest>> GetQuest(int id)
        {
            _logger.LogInformation("Fetching quest with ID: {QuestId}", id);

            var quest = await _context.Quests.FindAsync(id);
            if (quest == null)
            {
                _logger.LogWarning("Quest not found for ID: {QuestId}", id);
                return NotFound();
            }

            return Ok(quest);
        }

        // PUT: api/Quest/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutQuest(int id, Quest quest)
        {
            if (id != quest.Id)
            {
                _logger.LogWarning("PUT request failed. Mismatched IDs: {RequestId} != {QuestId}", id, quest.Id);
                return BadRequest();
            }

            _context.Entry(quest).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Quest updated successfully. ID: {QuestId}", id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuestExists(id))
                {
                    _logger.LogWarning("Update failed. Quest not found for ID: {QuestId}", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError("Error occurred while updating quest with ID: {QuestId}", id);
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Quest
        [HttpPost]
        public async Task<ActionResult<Quest>> PostQuest(Quest quest)
        {
            _logger.LogInformation("Creating new quest: {QuestTitle}", quest.Title);

            _context.Quests.Add(quest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Quest successfully created. ID: {QuestId}, Title: {QuestTitle}", quest.Id, quest.Title);
            return CreatedAtAction("GetQuest", new { id = quest.Id }, quest);
        }

        // DELETE: api/Quest/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuest(int id)
        {
            _logger.LogInformation("Attempting to delete quest with ID: {QuestId}", id);

            var quest = await _context.Quests.FindAsync(id);
            if (quest == null)
            {
                _logger.LogWarning("Deletion failed. Quest not found for ID: {QuestId}", id);
                return NotFound();
            }

            _context.Quests.Remove(quest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Quest successfully deleted. ID: {QuestId}", id);
            return NoContent();
        }

        private bool QuestExists(int id)
        {
            return _context.Quests.Any(e => e.Id == id);
        }
    }
}
