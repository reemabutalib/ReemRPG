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

        // Add this method to your QuestController class
        private string GetUserId()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claimValue))
            {
                return null;
            }
            return claimValue;
        }

        // Helper to get/create user character
        // Update the helper to get/create user character with better error handling
        private async Task<UserCharacter> GetOrCreateUserCharacterAsync(string userId, int characterId)
        {
            try
            {
                // First check if the user character already exists
                var userCharacter = await _context.UserCharacters
                    .Include(uc => uc.Character)
                    .FirstOrDefaultAsync(uc =>
                        uc.UserId == userId &&
                        uc.CharacterId == characterId);

                if (userCharacter != null)
                {
                    _logger.LogInformation("GetOrCreateUserCharacter: Found existing character {CharacterId} for user {UserId}",
                        characterId, userId);
                    return userCharacter;
                }

                // Verify the character exists in the Characters table
                var baseCharacter = await _context.Characters.FindAsync(characterId);
                if (baseCharacter == null)
                {
                    _logger.LogWarning("GetOrCreateUserCharacter: Character {CharacterId} not found in Characters table",
                        characterId);
                    return null;
                }

                // Verify the user exists in the AspNetUsers table
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("GetOrCreateUserCharacter: User {UserId} not found in AspNetUsers table", userId);
                    return null;
                }

                // Create a new UserCharacter entity
                _logger.LogInformation("GetOrCreateUserCharacter: Creating new UserCharacter for {UserId} and {CharacterId}",
                    userId, characterId);

                userCharacter = new UserCharacter
                {
                    UserId = userId,
                    CharacterId = characterId,
                    Level = 1,
                    Experience = 0,
                    Gold = 0,
                    IsSelected = false
                };

                // Add it to the context
                _context.UserCharacters.Add(userCharacter);

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("GetOrCreateUserCharacter: Successfully created UserCharacter");

                    // Load the character navigation property
                    await _context.Entry(userCharacter)
                        .Reference(uc => uc.Character)
                        .LoadAsync();

                    return userCharacter;
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "GetOrCreateUserCharacter: Database error when creating UserCharacter. UserId: {UserId}, CharacterId: {CharacterId}",
                        userId, characterId);

                    // Log more detailed info about the keys
                    var characterExists = await _context.Characters.AnyAsync(c => c.CharacterId == characterId);
                    var userExists = await _context.Users.AnyAsync(u => u.Id == userId);

                    _logger.LogError("Character {CharacterId} exists: {CharacterExists}, User {UserId} exists: {UserExists}",
                        characterId, characterExists, userId, userExists);

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrCreateUserCharacterAsync");
                throw;
            }
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

                _logger.LogInformation("AttemptQuest: Attempting quest {QuestId} with character {CharacterId}",
                    model.QuestId, model.CharacterId);

                // Get the current user ID, checking both Id and Email
                var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(claimValue))
                {
                    _logger.LogWarning("AttemptQuest: No user identifier claim found");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Look up user by either ID or email
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Id == claimValue || u.Email == claimValue);

                if (user == null)
                {
                    _logger.LogWarning("AttemptQuest: User with identifier {Identifier} not found in database", claimValue);
                    return NotFound(new { message = "User not found in database" });
                }

                // Use the actual user ID going forward
                var userId = user.Id;
                _logger.LogInformation("AttemptQuest: Found user with identifier {Identifier}, using ID: {UserId}",
                    claimValue, userId);

                // Get the quest
                var quest = await _context.Quests.FindAsync(model.QuestId);
                if (quest == null)
                {
                    _logger.LogWarning("AttemptQuest: Quest {QuestId} not found", model.QuestId);
                    return NotFound(new { message = "Quest not found" });
                }

                _logger.LogInformation("AttemptQuest: Found quest {QuestId}: {QuestTitle}",
                    quest.Id, quest.Title);

                // Verify character exists in the Characters table first
                var character = await _context.Characters.FindAsync(model.CharacterId);
                if (character == null)
                {
                    _logger.LogWarning("AttemptQuest: Character {CharacterId} not found in Characters table", model.CharacterId);
                    return NotFound(new { message = "Character not found" });
                }

                // Get the user character - don't try to create it here
                var userCharacter = await _context.UserCharacters
                    .Include(uc => uc.Character)
                    .FirstOrDefaultAsync(uc =>
                        uc.UserId == userId &&
                        uc.CharacterId == model.CharacterId);

                if (userCharacter == null)
                {
                    _logger.LogWarning("AttemptQuest: Character {CharacterId} not assigned to user {UserId}",
                        model.CharacterId, userId);
                    return NotFound(new { message = "This character doesn't belong to you. Please select this character first." });
                }

                _logger.LogInformation("AttemptQuest: Found character {CharacterId}: {CharacterName}, Level {Level}",
                    userCharacter.CharacterId, userCharacter.Character.Name, userCharacter.Level);

                // Check if character meets level requirement
                if (userCharacter.Level < quest.RequiredLevel)
                {
                    _logger.LogInformation("AttemptQuest: Character level {CharLevel} is less than required level {RequiredLevel}",
                        userCharacter.Level, quest.RequiredLevel);
                    return BadRequest(new
                    {
                        message = $"Your character needs to be level {quest.RequiredLevel} to attempt this quest."
                    });
                }

                // Check if quest was already completed
                var alreadyCompleted = await _context.QuestCompletions
                    .AnyAsync(qc =>
                        qc.UserId == userId &&
                        qc.CharacterId == model.CharacterId &&
                        qc.QuestId == model.QuestId);

                if (alreadyCompleted)
                {
                    _logger.LogInformation("AttemptQuest: Quest {QuestId} already completed by character {CharacterId}",
                        model.QuestId, model.CharacterId);
                    // Optionally return success with a different message
                    return Ok(new
                    {
                        success = true,
                        message = "You've already completed this quest!"
                    });
                }

                // Start a database transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
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
                    int oldLevel = userCharacter.Level;
                    userCharacter.Experience += quest.ExperienceReward;
                    userCharacter.Gold += quest.GoldReward;

                    // Check for level up
                    while (userCharacter.Experience >= (userCharacter.Level * 1000))
                    {
                        userCharacter.Level++;
                        _logger.LogInformation("AttemptQuest: Character {CharacterId} leveled up to {Level}",
                            model.CharacterId, userCharacter.Level);
                    }

                    bool leveledUp = userCharacter.Level > oldLevel;

                    // Add the quest completion
                    _context.QuestCompletions.Add(questCompletion);

                    // Save changes
                    await _context.SaveChangesAsync();

                    // Commit the transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("AttemptQuest: Successfully completed quest {QuestId} with character {CharacterId}",
                        model.QuestId, model.CharacterId);

                    // Return result
                    return Ok(new
                    {
                        success = true,
                        experienceGained = quest.ExperienceReward,
                        goldGained = quest.GoldReward,
                        levelUp = leveledUp,
                        newLevel = userCharacter.Level,
                        currentExp = userCharacter.Experience,
                        expToNextLevel = (userCharacter.Level * 1000) - userCharacter.Experience,
                        message = leveledUp ?
                            $"Quest completed! You leveled up to level {userCharacter.Level}!" :
                            "Quest completed successfully!"
                    });
                }
                catch (Exception innerEx)
                {
                    // Roll back the transaction on error
                    await transaction.RollbackAsync();

                    _logger.LogError(innerEx, "AttemptQuest: Database error during quest attempt. Quest: {QuestId}, Character: {CharacterId}",
                        model.QuestId, model.CharacterId);

                    throw; // Re-throw to be caught by outer try-catch
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AttemptQuest: Error processing quest attempt. Message: {Message}, Stack: {StackTrace}",
                    ex.Message, ex.StackTrace);

                return StatusCode(500, new
                {
                    message = "Error attempting quest",
                    details = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // GET: api/Quest/character/{characterId}/completed - Get quests completed by character
        [HttpGet("character/{characterId}/completed")]
        [Authorize]
        public async Task<IActionResult> GetCharacterCompletedQuests(int characterId)
        {
            try
            {
                // Get the current user ID, checking both Id and Email
                var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(claimValue))
                {
                    _logger.LogWarning("GetCharacterCompletedQuests: No user identifier claim found");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Look up user by either ID or email
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Id == claimValue || u.Email == claimValue);

                if (user == null)
                {
                    _logger.LogWarning("GetCharacterCompletedQuests: User with identifier {Identifier} not found in database", claimValue);
                    return NotFound(new { message = "User not found in database" });
                }

                // Use the actual user ID going forward
                var userId = user.Id;
                _logger.LogInformation("GetCharacterCompletedQuests: Found user with identifier {Identifier}, using ID: {UserId}",
                    claimValue, userId);

                // Now look up the user character with the correct userId
                var userCharacter = await _context.UserCharacters
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

                if (userCharacter == null)
                {
                    _logger.LogWarning("GetCharacterCompletedQuests: Character {CharacterId} not found for user {UserId}",
                        characterId, userId);
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
                // Get the current user ID, checking both Id and Email
                var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(claimValue))
                {
                    _logger.LogWarning("GetCharacterProgress: No user identifier claim found");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Look up user by either ID or email
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Id == claimValue || u.Email == claimValue);

                if (user == null)
                {
                    _logger.LogWarning("GetCharacterProgress: User with identifier {Identifier} not found in database", claimValue);
                    return NotFound(new { message = "User not found in database" });
                }

                // Use the actual user ID going forward
                var userId = user.Id;
                _logger.LogInformation("GetCharacterProgress: Found user with identifier {Identifier}, using ID: {UserId}",
                    claimValue, userId);

                // Now look up the user character with the correct userId
                var userCharacter = await _context.UserCharacters
                    .Include(uc => uc.Character)
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

                if (userCharacter == null)
                {
                    _logger.LogWarning("GetCharacterProgress: Character {CharacterId} not found for user {UserId}",
                        characterId, userId);
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