using ReemRPG.Data;
using ReemRPG.Models;
using ReemRPG.DTOs;
using ReemRPG.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReemRPG.Services.Implementations
{
    public class UserCharacterService : IUserCharacterService
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<UserCharacterService> _logger;

        public UserCharacterService(ApplicationContext context, ILogger<UserCharacterService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Select a character for a user with proper progression data
        public async Task<bool> SelectCharacterAsync(string userId, int characterId)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("User ID cannot be null or empty");
                return false;
            }

            if (characterId <= 0)
            {
                _logger.LogWarning("Invalid character ID: {CharacterId}", characterId);
                return false;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verify user exists
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    _logger.LogWarning("User {UserId} not found in database", userId);
                    return false;
                }

                // Verify character exists
                var character = await _context.Characters.FindAsync(characterId);
                if (character == null)
                {
                    _logger.LogWarning("Character {CharacterId} not found in database", characterId);
                    return false;
                }

                // Check if association already exists
                var existingAssociation = await _context.UserCharacters
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

                if (existingAssociation == null)
                {
                    // Create new association
                    existingAssociation = new UserCharacter
                    {
                        UserId = userId,
                        CharacterId = characterId,
                        Level = 1,
                        Experience = 0,
                        Gold = 0,
                        IsSelected = true
                    };

                    _context.UserCharacters.Add(existingAssociation);
                }
                else
                {
                    existingAssociation.IsSelected = true;
                }

                // Mark any previously selected character as not selected
                var previousSelections = await _context.UserCharacters
                    .Where(uc => uc.UserId == userId && uc.CharacterId != characterId && uc.IsSelected)
                    .ToListAsync();

                foreach (var prev in previousSelections)
                {
                    prev.IsSelected = false;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully selected character {CharacterId} for user {UserId}",
                    characterId, userId);
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                _logger.LogError(dbEx, "Database error while selecting character {CharacterId} for user {UserId}",
                    characterId, userId);
                return false;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error while selecting character {CharacterId} for user {UserId}",
                    characterId, userId);
                return false;
            }
        }

        // Get character with user-specific progression
        public async Task<CharacterDTO> GetCharacterWithProgressionAsync(int id, string userId)
        {
            // Try to get user-specific character data first
            var userCharacter = await _context.UserCharacters
                .Include(uc => uc.Character)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == id);

            if (userCharacter == null)
            {
                // Character not associated with user yet, return base character with default progression
                var baseCharacter = await _context.Characters.FindAsync(id);
                if (baseCharacter == null)
                {
                    return null;
                }

                return new CharacterDTO
                {
                    CharacterId = baseCharacter.CharacterId,
                    Name = baseCharacter.Name,
                    Class = baseCharacter.Class,
                    ImageUrl = baseCharacter.ImageUrl,
                    BaseStrength = baseCharacter.BaseStrength,
                    BaseAgility = baseCharacter.BaseAgility,
                    BaseIntelligence = baseCharacter.BaseIntelligence,
                    BaseHealth = baseCharacter.BaseHealth,
                    Health = baseCharacter.BaseHealth + 10, // Default level 1 health
                    BaseAttackPower = baseCharacter.BaseAttackPower,
                    AttackPower = baseCharacter.BaseAttackPower + 2, // Default level 1 attack
                    Level = 1,
                    Experience = 0,
                    Gold = 0,
                    IsAssociatedWithUser = false
                };
            }

            // Return character with user-specific progression
            return new CharacterDTO
            {
                CharacterId = userCharacter.Character.CharacterId,
                Name = userCharacter.Character.Name,
                Class = userCharacter.Character.Class,
                ImageUrl = userCharacter.Character.ImageUrl,
                BaseStrength = userCharacter.Character.BaseStrength,
                BaseAgility = userCharacter.Character.BaseAgility,
                BaseIntelligence = userCharacter.Character.BaseIntelligence,
                BaseHealth = userCharacter.Character.BaseHealth,
                Health = userCharacter.Character.BaseHealth + (userCharacter.Level * 10),
                BaseAttackPower = userCharacter.Character.BaseAttackPower,
                AttackPower = userCharacter.Character.BaseAttackPower + (userCharacter.Level * 2),
                Level = userCharacter.Level,
                Experience = userCharacter.Experience,
                Gold = userCharacter.Gold,
                IsAssociatedWithUser = true
            };
        }

        // Get selected character with progression
        public async Task<CharacterDTO> GetSelectedCharacterAsync(string userId)
        {
            var selectedUserCharacter = await _context.UserCharacters
                .Include(uc => uc.Character)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.IsSelected);

            if (selectedUserCharacter == null)
            {
                _logger.LogInformation("No selected character found for user {UserId}", userId);
                return null;
            }

            return new CharacterDTO
            {
                CharacterId = selectedUserCharacter.Character.CharacterId,
                Name = selectedUserCharacter.Character.Name,
                Class = selectedUserCharacter.Character.Class,
                ImageUrl = selectedUserCharacter.Character.ImageUrl,
                BaseStrength = selectedUserCharacter.Character.BaseStrength,
                BaseAgility = selectedUserCharacter.Character.BaseAgility,
                BaseIntelligence = selectedUserCharacter.Character.BaseIntelligence,
                BaseHealth = selectedUserCharacter.Character.BaseHealth,
                Health = selectedUserCharacter.Character.BaseHealth + (selectedUserCharacter.Level * 10),
                BaseAttackPower = selectedUserCharacter.Character.BaseAttackPower,
                AttackPower = selectedUserCharacter.Character.BaseAttackPower + (selectedUserCharacter.Level * 2),
                Level = selectedUserCharacter.Level,
                Experience = selectedUserCharacter.Experience,
                Gold = selectedUserCharacter.Gold,
                IsAssociatedWithUser = true
            };
        }

        // Get all characters associated with a user, with progression data
        public async Task<IEnumerable<CharacterDTO>> GetCharactersByUserIdAsync(string userId)
        {
            var userCharacters = await _context.UserCharacters
                .Include(uc => uc.Character)
                .Where(uc => uc.UserId == userId)
                .Select(uc => new CharacterDTO
                {
                    CharacterId = uc.Character.CharacterId,
                    Name = uc.Character.Name,
                    Class = uc.Character.Class,
                    ImageUrl = uc.Character.ImageUrl,
                    BaseStrength = uc.Character.BaseStrength,
                    BaseAgility = uc.Character.BaseAgility,
                    BaseIntelligence = uc.Character.BaseIntelligence,
                    BaseHealth = uc.Character.BaseHealth,
                    Health = uc.Character.BaseHealth + (uc.Level * 10),
                    BaseAttackPower = uc.Character.BaseAttackPower,
                    AttackPower = uc.Character.BaseAttackPower + (uc.Level * 2),
                    Level = uc.Level,
                    Experience = uc.Experience,
                    Gold = uc.Gold,
                    IsAssociatedWithUser = true,
                    IsSelected = uc.IsSelected
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} characters for user {UserId}", userCharacters.Count, userId);

            return userCharacters;
        }

        // Update character progression for a user
        public async Task<(bool Success, bool LeveledUp, int NewLevel)> UpdateCharacterProgressionAsync(
            string userId, int characterId, int experienceGained, int goldGained)
        {
            var userCharacter = await _context.UserCharacters
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

            if (userCharacter == null)
            {
                _logger.LogWarning(
                    "Cannot update progression: Character {CharacterId} not associated with user {UserId}",
                    characterId, userId);
                return (false, false, 0);
            }

            // Update progression
            userCharacter.Experience += experienceGained;
            userCharacter.Gold += goldGained;

            // Handle level up
            int oldLevel = userCharacter.Level;
            while (userCharacter.Experience >= (userCharacter.Level * 1000))
            {
                userCharacter.Level++;
            }

            bool leveledUp = userCharacter.Level > oldLevel;
            if (leveledUp)
            {
                _logger.LogInformation(
                    "Character {CharacterId} for user {UserId} leveled up from {OldLevel} to {NewLevel}",
                    characterId, userId, oldLevel, userCharacter.Level);
            }

            await _context.SaveChangesAsync();
            return (true, leveledUp, userCharacter.Level);
        }

        // Get all available characters (both user-associated and not) - using CharacterDTO
        public async Task<IEnumerable<CharacterDTO>> GetAvailableCharactersAsync(string userId)
        {
            // Get all user's characters
            var userCharacterIds = await _context.UserCharacters
                .Where(uc => uc.UserId == userId)
                .Select(uc => uc.CharacterId)
                .ToListAsync();

            // Get user characters with progression data
            var userCharacters = await _context.UserCharacters
                .Include(uc => uc.Character)
                .Where(uc => uc.UserId == userId)
                .Select(uc => new CharacterDTO
                {
                    CharacterId = uc.Character.CharacterId,
                    Name = uc.Character.Name,
                    Class = uc.Character.Class,
                    ImageUrl = uc.Character.ImageUrl,
                    BaseStrength = uc.Character.BaseStrength,
                    BaseAgility = uc.Character.BaseAgility,
                    BaseIntelligence = uc.Character.BaseIntelligence,
                    BaseHealth = uc.Character.BaseHealth,
                    Health = uc.Character.BaseHealth + (uc.Level * 10),
                    BaseAttackPower = uc.Character.BaseAttackPower,
                    AttackPower = uc.Character.BaseAttackPower + (uc.Level * 2),
                    Level = uc.Level,
                    Experience = uc.Experience,
                    Gold = uc.Gold,
                    IsAssociatedWithUser = true,
                    IsSelected = uc.IsSelected
                })
                .ToListAsync();

            // Get characters that user doesn't have yet
            var otherCharacters = await _context.Characters
                .Where(c => !userCharacterIds.Contains(c.CharacterId))
                .Select(c => new CharacterDTO
                {
                    CharacterId = c.CharacterId,
                    Name = c.Name,
                    Class = c.Class,
                    ImageUrl = c.ImageUrl,
                    BaseStrength = c.BaseStrength,
                    BaseAgility = c.BaseAgility,
                    BaseIntelligence = c.BaseIntelligence,
                    BaseHealth = c.BaseHealth,
                    Health = c.BaseHealth + 10, // Level 1 health
                    BaseAttackPower = c.BaseAttackPower,
                    AttackPower = c.BaseAttackPower + 2, // Level 1 attack power
                    Level = 1,
                    Experience = 0,
                    Gold = 0,
                    IsAssociatedWithUser = false,
                    IsSelected = false
                })
                .ToListAsync();

            // Combine the lists
            return userCharacters.Concat(otherCharacters);
        }
    }
}