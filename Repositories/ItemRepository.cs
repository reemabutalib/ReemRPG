using ReemRPG.Models;
using ReemRPG.Repositories.Interfaces;

namespace ReemRPG.Repositories
{
    public class ItemRepository : Repository<Item>, IItemRepository
    {
        public ItemRepository(ApplicationContext context) : base(context) { }
    }
}
