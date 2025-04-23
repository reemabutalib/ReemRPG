using ReemRPG.Data;
using ReemRPG.Models;
using ReemRPG.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class CharacterService : ICharacterService
{
    private readonly ApplicationContext _context; // Correct context name
    private readonly ICharacterRepository _characterRepository;

    public CharacterService(ICharacterRepository characterRepository, ApplicationContext context)
    {
        _characterRepository = characterRepository;
        _context = context;
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

        _characterRepository.Add(character);
        await _characterRepository.SaveChangesAsync();

        return character;
    }

    public Task<Character?> UpdateCharacterAsync(int id, Character character)
    {
        throw new NotImplementedException();
    }

    Task<bool> ICharacterService.DeleteCharacterAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync()
    {
        var leaderboard = await _context.Characters
            .OrderByDescending(c => c.Experience)
            .Take(10)
            .Select(c => new LeaderboardEntry
            {
                CharacterName = c.Name,
                Class = c.Class,
                Experience = c.Experience,
                Level = c.Level
            })
            .ToListAsync();

        return leaderboard;
    }

    // Implementation for selecting a character
    public async Task<bool> SelectCharacterAsync(string userId, int characterId)
    {
        var character = await _context.Characters.FirstOrDefaultAsync(c => c.CharacterId == characterId);

        if (character == null)
        {
            return false; // Character not found
        }

        var existingSelection = await _context.UserCharacters
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

        if (existingSelection != null)
        {
            return true; // Character is already associated with the user
        }

        var userCharacter = new UserCharacter
        {
            UserId = userId,
            CharacterId = characterId
        };

        _context.UserCharacters.Add(userCharacter);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SaveSelectedCharacterAsync(string userId, int characterId)
    {
        var character = await _context.Characters.FirstOrDefaultAsync(c => c.CharacterId == characterId);

        if (character == null)
        {
            return false; // Character not found
        }

        var existingSelection = await _context.UserCharacters
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

        if (existingSelection != null)
        {
            return true; // Character is already associated with the user
        }

        var userCharacter = new UserCharacter
        {
            UserId = userId,
            CharacterId = characterId
        };

        _context.UserCharacters.Add(userCharacter);
        await _context.SaveChangesAsync();

        return true;
    }
    public async Task<Character> GetSelectedCharacterAsync(string userId)
    {
        var selectedCharacter = await _context.UserCharacters
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.Character)
            .FirstOrDefaultAsync();

        return selectedCharacter;
    }
}