using ReemRPG.Data;
using ReemRPG.Models;
using ReemRPG.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReemRPG.DTOs;

namespace ReemRPG.Services.Implementations
{
    public class CharacterService : ICharacterService
    {
        private readonly ApplicationContext _context;
        private readonly ICharacterRepository _characterRepository;
        private readonly ILogger<CharacterService> _logger;

        public CharacterService(ICharacterRepository characterRepository, ApplicationContext context, ILogger<CharacterService> logger)
        {
            _characterRepository = characterRepository;
            _context = context;
            _logger = logger;
        }

        // Get base character templates (for admin use)
        public async Task<IEnumerable<Character>> GetAllCharactersAsync()
        {
            // Debug logging to check what's happening
            var allCharacters = await _context.Characters.ToListAsync();
            _logger.LogInformation("Retrieved {Count} base characters from database", allCharacters.Count);

            return allCharacters;
        }

        // Get character base data
        public async Task<Character?> GetCharacterByIdAsync(int id)
        {
            return await _characterRepository.GetCharacterByIdAsync(id);
        }

        // Get character with user-specific progression - using CharacterDTO
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

        // Admin functions for base characters
        public async Task AddCharacterAsync(Character character)
        {
            await _characterRepository.AddCharacterAsync(character);
        }

        public async Task UpdateCharacterSimpleAsync(Character character)
        {
            await _characterRepository.UpdateCharacterAsync(character);
        }

        public async Task<bool> DeleteCharacterAsync(int id)
        {
            await _characterRepository.DeleteCharacterAsync(id);
            return true;
        }

        public async Task<Character> CreateCharacterAsync(Character character)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            _characterRepository.Add(character);
            await _characterRepository.SaveChangesAsync();

            return character;
        }

        // Update base character data (admin only)
        public async Task<Character?> UpdateCharacterAsync(int id, Character character)
        {
            // Implement this method
            var existingCharacter = await _context.Characters.FindAsync(id);
            if (existingCharacter == null)
                return null;

            // Update base character properties only
            existingCharacter.Name = character.Name;
            existingCharacter.Class = character.Class;
            existingCharacter.ImageUrl = character.ImageUrl;
            existingCharacter.BaseStrength = character.BaseStrength;
            existingCharacter.BaseAgility = character.BaseAgility;
            existingCharacter.BaseIntelligence = character.BaseIntelligence;
            existingCharacter.BaseHealth = character.BaseHealth;
            existingCharacter.BaseAttackPower = character.BaseAttackPower;

            await _context.SaveChangesAsync();
            return existingCharacter;
        }

        // Select a character for a user with proper progression data
        public async Task<bool> SelectCharacterAsync(string userId, int characterId)
        {
            var character = await _context.Characters.FirstOrDefaultAsync(c => c.CharacterId == characterId);

            if (character == null)
            {
                _logger.LogWarning("Character selection failed: Character {CharacterId} not found", characterId);
                return false;
            }

            var existingSelection = await _context.UserCharacters
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

            if (existingSelection != null)
            {
                _logger.LogInformation("Character {CharacterId} already associated with user {UserId}", characterId, userId);
                return true; // Character is already associated with the user
            }

            // Create new user character with initial progression values
            var userCharacter = new UserCharacter
            {
                UserId = userId,
                CharacterId = characterId,
                Level = 1,
                Experience = 0,
                Gold = 0
            };

            _context.UserCharacters.Add(userCharacter);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Associated character {CharacterId} with user {UserId}", characterId, userId);
            return true;
        }

        // Save selected character is now the same as SelectCharacter
        public async Task<bool> SaveSelectedCharacterAsync(string userId, int characterId)
        {
            return await SelectCharacterAsync(userId, characterId);
        }

        // Get selected character with progression - using CharacterDTO
        public async Task<CharacterDTO> GetSelectedCharacterAsync(string userId)
        {
            var selectedUserCharacter = await _context.UserCharacters
                .Include(uc => uc.Character)
                .FirstOrDefaultAsync(uc => uc.UserId == userId);

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

        // Get all characters associated with a user, with progression data - using CharacterDTO
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
                    IsAssociatedWithUser = true
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} characters for user {UserId}", userCharacters.Count, userId);

            return userCharacters;
        }

        // Update character progression for a user
        public async Task<bool> UpdateCharacterProgressionAsync(
            string userId, int characterId, int experienceGained, int goldGained)
        {
            var userCharacter = await _context.UserCharacters
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

            if (userCharacter == null)
            {
                _logger.LogWarning(
                    "Cannot update progression: Character {CharacterId} not associated with user {UserId}",
                    characterId, userId);
                return false;
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
            return true;
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
                    IsAssociatedWithUser = true
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
                    IsAssociatedWithUser = false
                })
                .ToListAsync();

            // Combine the lists
            return userCharacters.Concat(otherCharacters);
        }

        // Explicit interface implementation for the renamed method
        public async Task UpdateCharacterAsync(Character character)
        {
            await UpdateCharacterSimpleAsync(character);
        }
    }
}