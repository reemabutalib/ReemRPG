using ReemRPG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }
}