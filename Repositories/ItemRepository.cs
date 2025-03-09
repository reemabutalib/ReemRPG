using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ReemRPG.Models;
using ReemRPG.Repositories.Interfaces;

namespace ReemRPG.Repositories
{
    /// <summary>
    /// Implementation of the IItemRepository interface for handling database operations related to items.
    /// </summary>
    public class ItemRepository : IItemRepository
    {
        private readonly ApplicationContext _context;

        /// <summary>
        /// Constructor injecting the database context.
        /// </summary>
        public ItemRepository(ApplicationContext context)
        {
            _context = context;
        }

        // ------------------- Generic IRepository Implementation -------------------

        /// <summary>
        /// Retrieves all items from the database.
        /// </summary>
        public async Task<IEnumerable<Item>> GetAllAsync()
        {
            return await _context.Items.ToListAsync();
        }

        /// <summary>
        /// Retrieves an item by ID, returning null if not found.
        /// </summary>
        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items.FindAsync(id);
        }

        /// <summary>
        /// Adds a new item to the database and returns the added item.
        /// </summary>
        public async Task<Item> AddAsync(Item item)
        {
            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();
            return item;
        }

        /// <summary>
        /// Updates an existing item and returns the updated item if found, otherwise null.
        /// </summary>
        public async Task<Item?> UpdateAsync(Item item)
        {
            var existingItem = await _context.Items.FindAsync(item.Id);
            if (existingItem == null) return null;

            _context.Entry(existingItem).CurrentValues.SetValues(item);
            await _context.SaveChangesAsync();
            return existingItem;
        }

        /// <summary>
        /// Deletes an item by ID and returns true if deletion was successful.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return false;

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        // ------------------- Custom IItemRepository Methods -------------------

        /// <summary>
        /// Retrieves all items from the database.
        /// </summary>
        public async Task<IEnumerable<Item>> GetAllItemsAsync()
        {
            return await _context.Items.ToListAsync();
        }

        /// <summary>
        /// Retrieves an item by its ID.
        /// </summary>
        public async Task<Item?> GetItemByIdAsync(int id)
        {
            return await _context.Items.FindAsync(id);
        }

        /// <summary>
        /// Retrieves all items of a specific type (e.g., "Weapon", "Armor").
        /// </summary>
        public async Task<IEnumerable<Item>> GetItemsByTypeAsync(string type)
        {
            return await _context.Items.Where(i => i.Type == type).ToListAsync();
        }

        /// <summary>
        /// Adds a new item and returns the added item.
        /// </summary>
        public async Task<Item> AddItemAsync(Item item)
        {
            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();
            return item;
        }

        /// <summary>
        /// Updates an existing item, returning the updated item if found.
        /// </summary>
        public async Task<Item?> UpdateItemAsync(Item item)
        {
            var existingItem = await _context.Items.FindAsync(item.Id);
            if (existingItem == null) return null;

            _context.Entry(existingItem).CurrentValues.SetValues(item);
            await _context.SaveChangesAsync();
            return existingItem;
        }

        /// <summary>
        /// Deletes an item and returns true if deletion was successful.
        /// </summary>
        public async Task<bool> DeleteItemAsync(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return false;

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
