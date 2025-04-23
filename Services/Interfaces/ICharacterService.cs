namespace ReemRPG.Services.Interfaces
{
    using ReemRPG.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ICharacterService
    {
        Task<IEnumerable<Character>> GetAllCharactersAsync();
        Task<Character?> GetCharacterByIdAsync(int id);
        Task<Character> CreateCharacterAsync(Character character);
        Task<Character?> UpdateCharacterAsync(int id, Character character);
        Task<bool> DeleteCharacterAsync(int id);
        Task<List<LeaderboardEntry>> GetLeaderboardAsync();
        Task<bool> SelectCharacterAsync(string userId, int characterId);
        Task<bool> SaveSelectedCharacterAsync(string userId, int characterId);
        Task<Character> GetSelectedCharacterAsync(string userId);

    }
}
