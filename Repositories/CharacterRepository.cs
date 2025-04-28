using Microsoft.EntityFrameworkCore;
using ReemRPG.Models;
using ReemRPG.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReemRPG.Data;

public class CharacterRepository : ICharacterRepository
{
    private readonly ApplicationContext _context;

    public CharacterRepository(ApplicationContext context)
    {
        _context = context;
    }

    // Fetch all base characters (for admin/catalog purposes)
    public async Task<IEnumerable<Character>> GetAllCharactersAsync()
    {
        return await _context.Characters
            .AsNoTracking() // Optimized for read-only queries
            .ToListAsync();
    }

    // Get all characters available to a specific user
    // including their progression data
    public async Task<IEnumerable<object>> GetCharactersForUserAsync(string userId)
    {
        // First, get all characters the user has already selected
        var userCharacters = await _context.UserCharacters
            .Where(uc => uc.UserId == userId)
            .Include(uc => uc.Character)
            .Select(uc => new
            {
                uc.Character.CharacterId,
                uc.Character.Name,
                uc.Character.Class,
                uc.Character.ImageUrl,
                uc.Character.BaseStrength,
                uc.Character.BaseAgility,
                uc.Character.BaseIntelligence,
                uc.Character.BaseHealth,
                uc.Character.BaseAttackPower,
                Level = uc.Level,
                Experience = uc.Experience,
                Gold = uc.Gold,
                TotalHealth = uc.Character.BaseHealth + (uc.Level * 10),
                TotalAttackPower = uc.Character.BaseAttackPower + (uc.Level * 2),
                IsUserCharacter = true
            })
            .ToListAsync();

        // Then, get all available base characters the user hasn't selected yet
        var allCharacterIds = await _context.Characters.Select(c => c.CharacterId).ToListAsync();
        var userCharacterIds = userCharacters.Select(uc => uc.CharacterId).ToList();
        var unselectedCharacterIds = allCharacterIds.Except(userCharacterIds).ToList();

        var unselectedCharacters = await _context.Characters
            .Where(c => unselectedCharacterIds.Contains(c.CharacterId))
            .Select(c => new
            {
                c.CharacterId,
                c.Name,
                c.Class,
                c.ImageUrl,
                c.BaseStrength,
                c.BaseAgility,
                c.BaseIntelligence,
                c.BaseHealth,
                c.BaseAttackPower,
                Level = 1, // Default level for unselected characters
                Experience = 0, // Default experience
                Gold = 0, // Default gold
                TotalHealth = c.BaseHealth + 10, // Base + level 1 bonus
                TotalAttackPower = c.BaseAttackPower + 2, // Base + level 1 bonus
                IsUserCharacter = false
            })
            .ToListAsync();

        // Combine both lists
        return userCharacters.Concat(unselectedCharacters.Cast<object>());
    }

    // Fetch character with user-specific progression data
    public async Task<object> GetCharacterWithProgressionAsync(int characterId, string userId)
    {
        // Try to get user-specific character data first
        var userCharacter = await _context.UserCharacters
            .Include(uc => uc.Character)
            .Where(uc => uc.UserId == userId && uc.CharacterId == characterId)
            .Select(uc => new
            {
                uc.Character.CharacterId,
                uc.Character.Name,
                uc.Character.Class,
                uc.Character.ImageUrl,
                uc.Character.BaseStrength,
                uc.Character.BaseAgility,
                uc.Character.BaseIntelligence,
                uc.Character.BaseHealth,
                uc.Character.BaseAttackPower,
                Level = uc.Level,
                Experience = uc.Experience,
                Gold = uc.Gold,
                TotalHealth = uc.Character.BaseHealth + (uc.Level * 10),
                TotalAttackPower = uc.Character.BaseAttackPower + (uc.Level * 2),
                IsUserCharacter = true
            })
            .FirstOrDefaultAsync();

        if (userCharacter != null)
            return userCharacter;

        // If not found as user character, return base character data
        return await _context.Characters
            .Where(c => c.CharacterId == characterId)
            .Select(c => new
            {
                c.CharacterId,
                c.Name,
                c.Class,
                c.ImageUrl,
                c.BaseStrength,
                c.BaseAgility,
                c.BaseIntelligence,
                c.BaseHealth,
                c.BaseAttackPower,
                Level = 1, // Default level
                Experience = 0, // Default experience
                Gold = 0, // Default gold
                TotalHealth = c.BaseHealth + 10, // Base + level 1 bonus
                TotalAttackPower = c.BaseAttackPower + 2, // Base + level 1 bonus
                IsUserCharacter = false
            })
            .FirstOrDefaultAsync();
    }

    // Get base character by ID (keep existing method for admin use)
    public async Task<Character?> GetCharacterByIdAsync(int id)
    {
        return await _context.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CharacterId == id);
    }

    // Associate character with user (create or update progression)
    public async Task<bool> AssociateCharacterWithUserAsync(int characterId, string userId)
    {
        // Check if character exists
        var character = await _context.Characters.FindAsync(characterId);
        if (character == null)
            return false;

        // Check if association already exists
        var existingAssociation = await _context.UserCharacters
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

        if (existingAssociation == null)
        {
            // Create new association with default progress values
            var userCharacter = new UserCharacter
            {
                UserId = userId,
                CharacterId = characterId,
                Level = 1,
                Experience = 0,
                Gold = 0
            };

            await _context.UserCharacters.AddAsync(userCharacter);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // Update character progress
    public async Task<bool> UpdateCharacterProgressAsync(
        int characterId, string userId, int experienceGained, int goldGained)
    {
        var userCharacter = await _context.UserCharacters
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CharacterId == characterId);

        if (userCharacter == null)
            return false;

        // Update progression
        userCharacter.Experience += experienceGained;
        userCharacter.Gold += goldGained;

        // Check for level up
        int oldLevel = userCharacter.Level;
        while (userCharacter.Experience >= (userCharacter.Level * 1000))
        {
            userCharacter.Level++;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // Keep existing methods for base character management
    public async Task AddCharacterAsync(Character character)
    {
        await _context.Characters.AddAsync(character);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCharacterAsync(Character character)
    {
        _context.Characters.Update(character);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCharacterAsync(int id)
    {
        var character = await _context.Characters.FindAsync(id);
        if (character != null)
        {
            _context.Characters.Remove(character);
            await _context.SaveChangesAsync();
        }
    }

    public void Add(Character character)
    {
        _context.Characters.Add(character);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}