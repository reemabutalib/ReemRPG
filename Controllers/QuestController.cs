using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReemRPG.Data;
using ReemRPG.Models;

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

        // Helper method to get user ID
        private string GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return null;
            }
            return userIdClaim.Value;
        }

        // Helper to get/create user character
        private async Task<UserCharacter> GetOrCreateUserCharacterAsync(string userId, int characterId)
        {
            var userCharacter = await _context.UserCharacters
                .Include(uc => uc.Character)
                .FirstOrDefaultAsync(uc =>
                    uc.UserId == userId &&
                    uc.CharacterId == characterId);

            if (userCharacter == null)
            {
                var baseCharacter = await _context.Characters
                    .FindAsync(characterId);

                if (baseCharacter == null)
                {
                    return null;
                }

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
            }

            return userCharacter;
        }

        // GET: api/Quest - Get all available quests
        [HttpGet]
        public async Task<IActionResult> GetQuests()
        {
            try
            {
                var quests = await _context.Quests
                    .Select(q => new
                    {
                        questId = q.Id,
                        title = q.Title,
                        description = q.Description,
                        experienceReward = q.ExperienceReward,
                        goldReward = q.GoldReward,
                        requiredLevel = q.RequiredLevel
                    })
                    .ToListAsync();

                return Ok(quests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quests");
                return StatusCode(500, new { message = "Error retrieving quests" });
            }
        }

        // GET: api/Quest/{id} - Get a specific quest
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuest(int id)
        {
            try
            {
                var quest = await _context.Quests.FindAsync(id);
                if (quest == null)
                {
                    return NotFound(new { message = "Quest not found" });
                }

                return Ok(new
                {
                    questId = quest.Id,
                    title = quest.Title,
                    description = quest.Description,
                    experienceReward = quest.ExperienceReward,
                    goldReward = quest.GoldReward,
                    requiredLevel = quest.RequiredLevel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quest {QuestId}", id);
                return StatusCode(500, new { message = "Error retrieving quest" });
            }
        }

        // POST: api/Quest/attempt - Attempt a quest with selected character
        [HttpPost("attempt")]
        [Authorize]
        public async Task<IActionResult> AttemptQuest([FromBody] QuestAttemptModel model)
        {
            try
            {
                // Check request validity
                if (model == null || model.QuestId <= 0 || model.CharacterId <= 0)
                {
                    return BadRequest(new { message = "Invalid request" });
                }

                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Get the quest
                var quest = await _context.Quests.FindAsync(model.QuestId);
                if (quest == null)
                {
                    return NotFound(new { message = "Quest not found" });
                }

                // Get or create the user character
                var userCharacter = await GetOrCreateUserCharacterAsync(userId, model.CharacterId);
                if (userCharacter == null)
                {
                    return NotFound(new { message = "Character not found" });
                }

                // Record completion
                var questCompletion = new QuestCompletion
                {
                    QuestId = model.QuestId,
                    CharacterId = model.CharacterId,
                    UserId = userId,
                    CompletedOn = DateTime.UtcNow,
                    ExperienceGained = quest.ExperienceReward,
                    GoldGained = quest.GoldReward
                };

                // Update character progression
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

                // Return result
                return Ok(new
                {
                    success = true,
                    experienceGained = quest.ExperienceReward,
                    goldGained = quest.GoldReward,
                    levelUp = leveledUp,
                    newLevel = userCharacter.Level,
                    message = leveledUp ?
                        $"Quest completed! You leveled up to level {userCharacter.Level}!" :
                        "Quest completed successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attempting quest");
                return StatusCode(500, new { message = "Error attempting quest" });
            }
        }

        // GET: api/Quest/character/{characterId}/completed - Get quests completed by character
        [HttpGet("character/{characterId}/completed")]
        [Authorize]
        public async Task<IActionResult> GetCharacterCompletedQuests(int characterId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var userCharacter = await _context.UserCharacters
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

                if (userCharacter == null)
                {
                    return NotFound(new { message = "Character not found or doesn't belong to you" });
                }

                var completedQuests = await _context.QuestCompletions
                    .Where(qc => qc.UserId == userId && qc.CharacterId == characterId)
                    .Join(_context.Quests,
                          qc => qc.QuestId,
                          q => q.Id,
                          (qc, q) => new
                          {
                              completionId = qc.Id,
                              questId = q.Id,
                              title = q.Title,
                              completedOn = qc.CompletedOn,
                              experienceGained = qc.ExperienceGained,
                              goldGained = qc.GoldGained
                          })
                    .ToListAsync();

                return Ok(completedQuests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching character completed quests");
                return StatusCode(500, new { message = "Error retrieving completed quests" });
            }
        }

        // GET: api/Quest/character/progress/{characterId} - Get character progress
        [HttpGet("character/progress/{characterId}")]
        [Authorize]
        public async Task<IActionResult> GetCharacterProgress(int characterId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var userCharacter = await _context.UserCharacters
                    .Include(uc => uc.Character)
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

                if (userCharacter == null)
                {
                    return NotFound(new { message = "Character not found or doesn't belong to you" });
                }

                var questsCompleted = await _context.QuestCompletions
                    .CountAsync(qc => qc.UserId == userId && qc.CharacterId == characterId);

                var totalExp = await _context.QuestCompletions
                    .Where(qc => qc.UserId == userId && qc.CharacterId == characterId)
                    .SumAsync(qc => qc.ExperienceGained);

                var totalGold = await _context.QuestCompletions
                    .Where(qc => qc.UserId == userId && qc.CharacterId == characterId)
                    .SumAsync(qc => qc.GoldGained);

                return Ok(new
                {
                    characterId = userCharacter.CharacterId,
                    name = userCharacter.Character.Name,
                    class_ = userCharacter.Character.Class,
                    level = userCharacter.Level,
                    experience = userCharacter.Experience,
                    experienceToNextLevel = (userCharacter.Level * 1000) - userCharacter.Experience,
                    gold = userCharacter.Gold,
                    questsCompleted = questsCompleted,
                    totalExperienceGained = totalExp,
                    totalGoldGained = totalGold
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching character progress");
                return StatusCode(500, new { message = "Error retrieving character progress" });
            }
        }

        // For admin use only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateQuest(Quest quest)
        {
            try
            {
                _context.Quests.Add(quest);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetQuest), new { id = quest.Id }, quest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quest");
                return StatusCode(500, new { message = "Error creating quest" });
            }
        }

        // For admin use only
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateQuest(int id, Quest quest)
        {
            if (id != quest.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            try
            {
                _context.Entry(quest).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex) when (ex is DbUpdateConcurrencyException)
            {
                if (!await _context.Quests.AnyAsync(q => q.Id == id))
                {
                    return NotFound(new { message = "Quest not found" });
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quest");
                return StatusCode(500, new { message = "Error updating quest" });
            }
        }
    }
}