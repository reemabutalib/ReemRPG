using System.Collections.Generic;
using System.Threading.Tasks;
using ReemRPG.Models;

namespace ReemRPG.Services.Interfaces
{
    public interface IItemService
    {
        Task<IEnumerable<Item>> GetAllItemsAsync();
        Task<Item?> GetItemByIdAsync(int id);
        Task<Item> CreateItemAsync(Item item);
        Task<Item?> UpdateItemAsync(int id, Item item);
        Task<bool> DeleteItemAsync(int id);
    }
}
