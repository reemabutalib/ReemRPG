using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ReemRPG.Models;
using ReemRPG.Services.Interfaces;
using ReemRPG.Data;

public class ItemService : IItemService
{
    private readonly ApplicationContext _context;

    public ItemService(ApplicationContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Item>> GetAllItemsAsync()
    {
        return await _context.Items.ToListAsync();
    }

    public async Task<Item?> GetItemByIdAsync(int id)
    {
        return await _context.Items.FindAsync(id);
    }

    public async Task<Item> CreateItemAsync(Item item)
    {
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<Item?> UpdateItemAsync(int id, Item item)
    {
        var existingItem = await _context.Items.FindAsync(id);
        if (existingItem == null) return null;

        existingItem.Name = item.Name;
        existingItem.Type = item.Type;
        existingItem.Description = item.Description;
        existingItem.Value = item.Value;
        existingItem.AttackBonus = item.AttackBonus;
        existingItem.DefenseBonus = item.DefenseBonus;
        existingItem.HealthRestore = item.HealthRestore;

        await _context.SaveChangesAsync();
        return existingItem;
    }

    public async Task<bool> DeleteItemAsync(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null) return false;

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }
}
