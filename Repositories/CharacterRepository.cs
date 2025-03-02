using Microsoft.EntityFrameworkCore;
using ReemRPG.Models;
using ReemRPG.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CharacterRepository : ICharacterRepository
{
    private readonly ApplicationContext _context;

    public CharacterRepository(ApplicationContext context)
    {
        _context = context;
    }

    //  Fetch all characters with their related items
    public async Task<IEnumerable<Character>> GetAllCharactersAsync()
    {
        return await _context.Characters
            .AsNoTracking() //  Optimized for read-only queries
            .Include(c => c.Items) // Eager loading for performance
            .ToListAsync();
    }

    // ✅ Fetch a character by ID with related items
    public async Task<Character?> GetCharacterByIdAsync(int id)
    {
        return await _context.Characters
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CharacterId == id);
    }

    // Add a new character
    public async Task AddCharacterAsync(Character character)
    {
        await _context.Characters.AddAsync(character);
        await _context.SaveChangesAsync();
    }

    // Update a character
    public async Task UpdateCharacterAsync(Character character)
    {
        _context.Characters.Update(character);
        await _context.SaveChangesAsync();
    }

    // Delete a character
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
