using ReemRPG.Models;
using ReemRPG.Services.Interfaces;

public class CharacterService : ICharacterService
{
    // Follows LSP: ICharacterRepository interface is used so that any repository can be used interchangeably 
    private readonly ICharacterRepository _characterRepository;

    public CharacterService(ICharacterRepository characterRepository)
    {
        _characterRepository = characterRepository;
    }

    public async Task<IEnumerable<Character>> GetAllCharactersAsync()
    {
        return await _characterRepository.GetAllCharactersAsync();
    }

    public async Task<Character?> GetCharacterByIdAsync(int id)
    {
        return await _characterRepository.GetCharacterByIdAsync(id);
    }

    public async Task AddCharacterAsync(Character character)
    {
        await _characterRepository.AddCharacterAsync(character);
    }

    public async Task UpdateCharacterAsync(Character character)
    {
        await _characterRepository.UpdateCharacterAsync(character);
    }

    public async Task DeleteCharacterAsync(int id)
    {
        await _characterRepository.DeleteCharacterAsync(id);
    }

    public async Task<Character> CreateCharacterAsync(Character character)
{
    if (character == null)
    {
        throw new ArgumentNullException(nameof(character));
    }

    _characterRepository.Add(character); // Call Add method
    await _characterRepository.SaveChangesAsync(); // Ensure this is awaited

    return character; // Return the created character
}

    public Task<Character?> UpdateCharacterAsync(int id, Character character)
    {
        throw new NotImplementedException();
    }

    Task<bool> ICharacterService.DeleteCharacterAsync(int id)
    {
        throw new NotImplementedException();
    }
}
