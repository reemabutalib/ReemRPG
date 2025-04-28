using ReemRPG.DTOs;
using ReemRPG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReemRPG.Services.Implementations;

namespace ReemRPG.Services.Interfaces
{
    public interface ICharacterService
    {
        // Base character management (admin)
        Task<IEnumerable<Character>> GetAllCharactersAsync();
        Task<Character?> GetCharacterByIdAsync(int id);
        Task AddCharacterAsync(Character character);
        Task UpdateCharacterAsync(Character character);
        Task<bool> DeleteCharacterAsync(int id);
        Task<Character> CreateCharacterAsync(Character character);
        Task<Character?> UpdateCharacterAsync(int id, Character character);

        // Character progression (user specific) - using CharacterDTO
        Task<CharacterDTO> GetCharacterWithProgressionAsync(int id, string userId);
        Task<bool> SelectCharacterAsync(string userId, int characterId);
        Task<bool> SaveSelectedCharacterAsync(string userId, int characterId);
        Task<CharacterDTO> GetSelectedCharacterAsync(string userId);
        Task<IEnumerable<CharacterDTO>> GetCharactersByUserIdAsync(string userId);
        Task<bool> UpdateCharacterProgressionAsync(string userId, int characterId, int experienceGained, int goldGained);
        Task<IEnumerable<CharacterDTO>> GetAvailableCharactersAsync(string userId);
    }
}