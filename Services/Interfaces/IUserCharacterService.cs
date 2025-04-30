using System.Collections.Generic;
using System.Threading.Tasks;
using ReemRPG.DTOs;

namespace ReemRPG.Services.Interfaces
{
    public interface IUserCharacterService
    {
        Task<bool> SelectCharacterAsync(string userId, int characterId);
        Task<CharacterDTO> GetCharacterWithProgressionAsync(int id, string userId);
        Task<CharacterDTO> GetSelectedCharacterAsync(string userId);
        Task<IEnumerable<CharacterDTO>> GetCharactersByUserIdAsync(string userId);
        Task<(bool Success, bool LeveledUp, int NewLevel)> UpdateCharacterProgressionAsync(
            string userId, int characterId, int experienceGained, int goldGained);
        Task<IEnumerable<CharacterDTO>> GetAvailableCharactersAsync(string userId);
    }
}