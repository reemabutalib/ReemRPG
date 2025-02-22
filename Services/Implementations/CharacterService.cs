using System.Collections.Generic;
using System.Threading.Tasks;
using ReemRPG.Models;
using ReemRPG.Repositories.Interfaces;
using ReemRPG.Services.Interfaces;

namespace ReemRPG.Services
{
    public class CharacterService : ICharacterService
    {
        private readonly ICharacterRepository _characterRepository;

        public CharacterService(ICharacterRepository characterRepository)
        {
            _characterRepository = characterRepository;
        }

        public async Task<IEnumerable<Character>> GetAllCharactersAsync()
        {
            return await _characterRepository.GetAllAsync();
        }

        public async Task<Character?> GetCharacterByIdAsync(int id)
        {
            return await _characterRepository.GetByIdAsync(id);
        }

        public async Task<Character> CreateCharacterAsync(Character character)
        {
            return await _characterRepository.AddAsync(character);
        }

        public async Task<Character?> UpdateCharacterAsync(int id, Character character)
        {
            return await _characterRepository.UpdateAsync(character);
        }

        public async Task<bool> DeleteCharacterAsync(int id)
        {
            return await _characterRepository.DeleteAsync(id);
        }
    }
}
