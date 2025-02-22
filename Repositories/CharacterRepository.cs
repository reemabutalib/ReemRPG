using ReemRPG.Models;
using ReemRPG.Repositories.Interfaces;

namespace ReemRPG.Repositories
{
    public class CharacterRepository : Repository<Character>, ICharacterRepository
    {
        public CharacterRepository(ApplicationContext context) : base(context) { }
    }
}
