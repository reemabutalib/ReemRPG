using ReemRPG.Data;
using ReemRPG.Models;
using ReemRPG.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            try
            {
                var character = await _context.Characters.FindAsync(id);
                if (character == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent character with ID {CharacterId}", id);
                    return false;
                }

                // Check if character is used by any users
                bool isInUse = await _context.UserCharacters.AnyAsync(uc => uc.CharacterId == id);
                if (isInUse)
                {
                    _logger.LogWarning("Cannot delete character {CharacterId} as it's in use by users", id);
                    return false;
                }

                _context.Characters.Remove(character);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted character {CharacterId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete character {CharacterId}", id);
                return false;
            }
        }

        public async Task<Character> CreateCharacterAsync(Character character)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            _characterRepository.Add(character);
            await _characterRepository.SaveChangesAsync();
            _logger.LogInformation("Created new character: {CharacterName} ({CharacterId})",
                character.Name, character.CharacterId);

            return character;
        }

        // Update base character data (admin only)
        public async Task<Character?> UpdateCharacterAsync(int id, Character character)
        {
            // Implement this method
            var existingCharacter = await _context.Characters.FindAsync(id);
            if (existingCharacter == null)
            {
                _logger.LogWarning("Attempted to update non-existent character with ID {CharacterId}", id);
                return null;
            }

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
            _logger.LogInformation("Updated character {CharacterId}", id);
            return existingCharacter;
        }

        // Explicit interface implementation for the renamed method
        public async Task UpdateCharacterAsync(Character character)
        {
            await UpdateCharacterSimpleAsync(character);
        }
    }
}