using ReemRPG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICharacterRepository
{
    void Add(Character character); 
    Task<IEnumerable<Character>> GetAllCharactersAsync();
    Task<Character?> GetCharacterByIdAsync(int id);
    Task AddCharacterAsync(Character character);
    Task UpdateCharacterAsync(Character character);
    Task DeleteCharacterAsync(int id);
    Task<int> SaveChangesAsync();
}
