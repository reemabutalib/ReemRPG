using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ReemRPG.Models;
using ReemRPG.Data;
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
        private readonly ILogger<QuestController> _logger;

        public QuestController(ApplicationContext context, ILogger<QuestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Helper method to get current user ID
        private string GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return null;
            }
            return userIdClaim.Value;
        }

        // Helper method to ensure user has access to character
        private async Task<UserCharacter> GetOrCreateUserCharacterAsync(string userId, int characterId)
        {
            // First try to get existing user character
            var userCharacter = await _context.UserCharacters
                .Include(uc => uc.Character)
                .FirstOrDefaultAsync(uc =>
                    uc.UserId == userId &&
                    uc.CharacterId == characterId);

            // If user doesn't have this character yet, verify base character exists and create association
            if (userCharacter == null)
            {
                var baseCharacter = await _context.Characters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CharacterId == characterId);

                if (baseCharacter == null)
                {
                    // Character doesn't exist at all
                    return null;
                }

                // Create user character association with default progression
                _logger.LogInformation($"Creating character association for user {userId}, character {characterId}");

                userCharacter = new UserCharacter
                {
                    UserId = userId,
                    CharacterId = characterId,
                    Level = 1,
                    Experience = 0,
                    Gold = 0,
                    Character = baseCharacter
                };

                _context.UserCharacters.Add(userCharacter);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully created character association for user {userId}, character {characterId}");
            }

            return userCharacter;
        }

        // GET: api/Quest
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Quest>>> GetQuests()
        {
            _logger.LogInformation("Fetching all quests.");
            var quests = await _context.Quests.ToListAsync();
            return Ok(quests);
        }

        // GET: api/Quest/completed
        [HttpGet("completed")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<QuestCompletionDTO>>> GetCompletedQuests()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("Request for completed quests failed: User not authenticated");
                    return Unauthorized(new { message = "User not authenticated." });
                }

                _logger.LogInformation("Fetching completed quests for user: {UserId}", userId);

                // Get all quest completions for the current user only
                var completions = await _context.QuestCompletions
                    .Where(qc => qc.UserId == userId)
                    .Include(qc => qc.Character)
                    .Select(qc => new QuestCompletionDTO
                    {
                        Id = qc.Id,
                        QuestId = qc.QuestId,
                        CharacterId = qc.CharacterId,
                        CharacterName = qc.Character.Name,
                        CompletedOn = qc.CompletedOn,
                        ExperienceGained = qc.ExperienceGained,
                        GoldGained = qc.GoldGained
                    })
                    .ToListAsync();

                return Ok(completions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching completed quests");
                return StatusCode(500, new { message = "An error occurred while fetching completed quests" });
            }
        }

        // GET: api/Quest/character/{characterId}/completed
        [HttpGet("character/{characterId}/completed")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<QuestCompletionDTO>>> GetCharacterCompletedQuests(int characterId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("Request for character quest completions failed: User not authenticated");
                    return Unauthorized(new { message = "User not authenticated." });
                }

                // Get or create character association
                var userCharacter = await GetOrCreateUserCharacterAsync(userId, characterId);

                if (userCharacter == null)
                {
                    _logger.LogWarning("Character {CharacterId} not found for user {UserId}", characterId, userId);
                    return NotFound(new { message = "Character not found" });
                }

                _logger.LogInformation("Fetching completed quests for character: {CharacterId}", characterId);

                // Get quest completions for the specific character AND current user
                var completions = await _context.QuestCompletions
                    .Where(qc => qc.CharacterId == characterId && qc.UserId == userId)
                    .Include(qc => qc.Character)
                    .Select(qc => new QuestCompletionDTO
                    {
                        Id = qc.Id,
                        QuestId = qc.QuestId,
                        CharacterId = qc.CharacterId,
                        CharacterName = qc.Character.Name,
                        CompletedOn = qc.CompletedOn,
                        ExperienceGained = qc.ExperienceGained,
                        GoldGained = qc.GoldGained
                    })
                    .ToListAsync();

                return Ok(completions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching character completed quests");
                return StatusCode(500, new { message = "An error occurred while fetching character completed quests" });
            }
        }

        // GET: api/Quest/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Quest>> GetQuest(int id)
        {
            try
            {
                _logger.LogInformation("Fetching quest with ID: {QuestId}", id);

                var quest = await _context.Quests.FindAsync(id);
                if (quest == null)
                {
                    _logger.LogWarning("Quest not found for ID: {QuestId}", id);
                    return NotFound(new { message = "Quest not found" });
                }

                return Ok(quest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quest {QuestId}", id);
                return StatusCode(500, new { message = "An error occurred while fetching quest details" });
            }
        }

        // GET: api/Quest/{id}/status
        [HttpGet("{id}/status")]
        [Authorize]
        public async Task<ActionResult<QuestStatusDTO>> GetQuestStatus(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated." });
                }

                // Get the quest
                var quest = await _context.Quests.FindAsync(id);
                if (quest == null)
                {
                    return NotFound(new { message = "Quest not found." });
                }

                // Check if the user has completed this quest with any character
                bool isCompleted = await _context.QuestCompletions
                    .AnyAsync(qc => qc.QuestId == id && qc.UserId == userId);

                // Get all user characters that have completed this quest
                var completedByCharacters = await _context.QuestCompletions
                    .Where(qc => qc.QuestId == id && qc.UserId == userId)
                    .Include(qc => qc.Character)
                    .Select(qc => new
                    {
                        qc.CharacterId,
                        CharacterName = qc.Character.Name,
                        qc.CompletedOn
                    })
                    .ToListAsync();

                return Ok(new QuestStatusDTO
                {
                    QuestId = id,
                    Title = quest.Title,
                    IsCompleted = isCompleted,
                    CompletedByCharacters = completedByCharacters.Select(c => new CharacterCompletionDTO
                    {
                        CharacterId = c.CharacterId,
                        CharacterName = c.CharacterName,
                        CompletedOn = c.CompletedOn
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quest status for quest {QuestId}", id);
                return StatusCode(500, new { message = "An error occurred while fetching quest status" });
            }
        }

        // PUT: api/Quest/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Restrict to admins
        public async Task<IActionResult> PutQuest(int id, Quest quest)
        {
            try
            {
                if (id != quest.Id)
                {
                    _logger.LogWarning("PUT request failed. Mismatched IDs: {RequestId} != {QuestId}", id, quest.Id);
                    return BadRequest(new { message = "ID mismatch" });
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
                        return NotFound(new { message = "Quest not found" });
                    }
                    else
                    {
                        _logger.LogError("Concurrency error occurred while updating quest with ID: {QuestId}", id);
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quest {QuestId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the quest" });
            }
        }

        // POST: api/Quest
        [HttpPost]
        [Authorize(Roles = "Admin")] // Restrict to admins
        public async Task<ActionResult<Quest>> PostQuest(Quest quest)
        {
            try
            {
                _logger.LogInformation("Creating new quest: {QuestTitle}", quest.Title);

                _context.Quests.Add(quest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Quest successfully created. ID: {QuestId}, Title: {QuestTitle}", quest.Id, quest.Title);
                return CreatedAtAction("GetQuest", new { id = quest.Id }, quest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quest");
                return StatusCode(500, new { message = "An error occurred while creating the quest" });
            }
        }

        // POST: api/Quest/attempt
        [HttpPost("attempt")]
        [Authorize]
        public async Task<IActionResult> AttemptQuest([FromBody] QuestAttemptModel model)
        {
            try
            {
                // Check request validity
                if (model == null || model.QuestId <= 0 || model.CharacterId <= 0)
                {
                    _logger.LogWarning("Invalid quest attempt: QuestId or CharacterId is invalid");
                    return BadRequest(new { message = "Invalid quest or character ID" });
                }

                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                _logger.LogInformation($"Quest attempt by user {userId} for character {model.CharacterId} on quest {model.QuestId}");

                // Get quest first to fail fast
                var quest = await _context.Quests.FindAsync(model.QuestId);
                if (quest == null)
                {
                    _logger.LogWarning($"Quest {model.QuestId} not found");
                    return NotFound(new { message = "Quest not found" });
                }

                // Get or create character association
                var userCharacter = await GetOrCreateUserCharacterAsync(userId, model.CharacterId);

                if (userCharacter == null)
                {
                    _logger.LogWarning($"Character {model.CharacterId} not found");
                    return BadRequest(new { message = "Character not found" });
                }

                // Check level requirement
                if (userCharacter.Level < quest.RequiredLevel)
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = $"This quest requires level {quest.RequiredLevel}."
                    });
                }

                // Check if quest was already completed by this character
                bool alreadyCompleted = await _context.QuestCompletions
                    .AnyAsync(qc =>
                        qc.QuestId == model.QuestId &&
                        qc.CharacterId == model.CharacterId &&
                        qc.UserId == userId);

                // Record quest completion
                var questCompletion = new QuestCompletion
                {
                    QuestId = model.QuestId,
                    CharacterId = model.CharacterId,
                    UserId = userId,
                    CompletedOn = DateTime.UtcNow,
                    ExperienceGained = quest.ExperienceReward,
                    GoldGained = quest.GoldReward
                };

                // Update character progression in UserCharacter
                userCharacter.Experience += quest.ExperienceReward;
                userCharacter.Gold += quest.GoldReward;

                // Check for level up
                int oldLevel = userCharacter.Level;
                while (userCharacter.Experience >= (userCharacter.Level * 1000))
                {
                    userCharacter.Level++;
                }

                bool leveledUp = userCharacter.Level > oldLevel;

                // Save changes
                _context.QuestCompletions.Add(questCompletion);
                await _context.SaveChangesAsync();

                // Return success with rewards
                return Ok(new
                {
                    Success = true,
                    ExperienceGained = quest.ExperienceReward,
                    GoldGained = quest.GoldReward,
                    LevelUp = leveledUp,
                    NewLevel = userCharacter.Level,
                    AlreadyCompleted = alreadyCompleted,
                    Message = alreadyCompleted ?
                        "Quest completed again! (Rewards still granted)" :
                        "Quest completed successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attempting quest");
                return StatusCode(500, new
                {
                    message = "An error occurred while attempting the quest",
                    error = ex.Message,
                    stackTrace = ex.StackTrace // Include for development only
                });
            }
        }

        // DELETE: api/Quest/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Restrict to admins
        public async Task<IActionResult> DeleteQuest(int id)
        {
            try
            {
                _logger.LogInformation("Attempting to delete quest with ID: {QuestId}", id);

                var quest = await _context.Quests.FindAsync(id);
                if (quest == null)
                {
                    _logger.LogWarning("Deletion failed. Quest not found for ID: {QuestId}", id);
                    return NotFound(new { message = "Quest not found" });
                }

                _context.Quests.Remove(quest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Quest successfully deleted. ID: {QuestId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quest {QuestId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the quest" });
            }
        }

        // DELETE: api/Quest/completion/{completionId}
        [HttpDelete("completion/{completionId}")]
        [Authorize]
        public async Task<IActionResult> DeleteQuestCompletion(int completionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated." });
                }

                // Get the completion record, ensuring it belongs to the current user
                var completion = await _context.QuestCompletions
                    .FirstOrDefaultAsync(qc => qc.Id == completionId && qc.UserId == userId);

                if (completion == null)
                {
                    return NotFound(new { message = "Quest completion record not found or does not belong to you." });
                }

                // Get the UserCharacter record to revert rewards
                var userCharacter = await _context.UserCharacters
                    .FirstOrDefaultAsync(uc =>
                        uc.UserId == userId &&
                        uc.CharacterId == completion.CharacterId);

                if (userCharacter != null)
                {
                    // Revert experience and gold in the UserCharacter record
                    userCharacter.Experience -= completion.ExperienceGained;
                    // Ensure it doesn't go negative
                    userCharacter.Experience = Math.Max(0, userCharacter.Experience);

                    userCharacter.Gold -= completion.GoldGained;
                    // Ensure it doesn't go negative
                    userCharacter.Gold = Math.Max(0, userCharacter.Gold);

                    // Adjust level if needed
                    while (userCharacter.Experience < ((userCharacter.Level - 1) * 1000) && userCharacter.Level > 1)
                    {
                        userCharacter.Level--;
                    }
                }

                // Remove the completion record
                _context.QuestCompletions.Remove(completion);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Quest completion record deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quest completion {CompletionId}", completionId);
                return StatusCode(500, new { message = "An error occurred while deleting the quest completion record" });
            }
        }

        private bool QuestExists(int id)
        {
            return _context.Quests.Any(e => e.Id == id);
        }
    }

    // DTOs for structured responses
    public class QuestCompletionDTO
    {
        public int Id { get; set; }
        public int QuestId { get; set; }
        public int CharacterId { get; set; }
        public string CharacterName { get; set; }
        public DateTime CompletedOn { get; set; }
        public int ExperienceGained { get; set; }
        public int GoldGained { get; set; }
    }

    public class QuestStatusDTO
    {
        public int QuestId { get; set; }
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
        public List<CharacterCompletionDTO> CompletedByCharacters { get; set; }
    }

    public class CharacterCompletionDTO
    {
        public int CharacterId { get; set; }
        public string CharacterName { get; set; }
        public DateTime CompletedOn { get; set; }
    }

    public class QuestAttemptModel
    {
        public int QuestId { get; set; }
        public int CharacterId { get; set; }
    }
}