using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly ApplicationContext _context;

        public InventoryController(ApplicationContext context)
        {
            _context = context;
        }

        // GET: api/Inventory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetInventories()
        {
            return await _context.Inventories.ToListAsync();
        }

        // GET: api/Inventory/5
        [HttpGet("{characterId}/{itemId}")]
        public async Task<ActionResult<Inventory>> GetInventory(int characterId, int itemId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.CharacterId == characterId && i.ItemId == itemId);


            if (inventory == null)
            {
                return NotFound();
            }

            return inventory;
        }

        // PUT: api/Inventory/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{characterId}/{itemId}")]
        public async Task<IActionResult> PutInventory(int characterId, int itemId, Inventory inventory)
        {
            if (characterId != inventory.CharacterId || itemId != inventory.ItemId)
            {
                return BadRequest();
            }


            _context.Entry(inventory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventoryExists(inventory.CharacterId, inventory.ItemId))

                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Inventory
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Inventory>> PostInventory(Inventory inventory)
        {
            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetInventory", new { characterId = inventory.CharacterId, itemId = inventory.ItemId }, inventory);

        }

        // DELETE: api/Inventory/5
        [HttpDelete("{characterId}/{itemId}")]
        public async Task<IActionResult> DeleteInventory(int characterId, int itemId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.CharacterId == characterId && i.ItemId == itemId);

            if (inventory == null)
            {
                return NotFound();
            }

            _context.Inventories.Remove(inventory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool InventoryExists(int characterId, int itemId)
{
    return _context.Inventories.Any(e => e.CharacterId == characterId && e.ItemId == itemId);
}

            
    }
}

