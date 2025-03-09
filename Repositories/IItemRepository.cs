using ReemRPG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReemRPG.Repositories.Interfaces
{
    /// <summary>
    /// Interface defining item-specific repository operations.
    /// Inherits from the generic IRepository interface.
    /// </summary>
    public interface IItemRepository : IRepository<Item>
    {
        /// <summary>
        /// Retrieves all items from the database asynchronously.
        /// </summary>
        Task<IEnumerable<Item>> GetAllItemsAsync();

        /// <summary>
        /// Retrieves a single item by its ID asynchronously.
        /// </summary>
        Task<Item?> GetItemByIdAsync(int id);

        /// <summary>
        /// Retrieves items by their type (e.g., "Weapon", "Armor").
        /// </summary>
        Task<IEnumerable<Item>> GetItemsByTypeAsync(string type);

        /// <summary>
        /// Adds a new item to the database and returns the created item.
        /// </summary>
        Task<Item> AddItemAsync(Item item);

        /// <summary>
        /// Updates an existing item and returns the updated item if found.
        /// </summary>
        Task<Item?> UpdateItemAsync(Item item);

        /// <summary>
        /// Deletes an item by its ID and returns true if successful.
        /// </summary>
        Task<bool> DeleteItemAsync(int id);
    }
}
