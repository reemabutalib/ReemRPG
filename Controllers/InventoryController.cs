using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Added for logging

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<InventoryController> _logger; // Inject ILogger

        public InventoryController(ApplicationContext context, ILogger<InventoryController> logger)
        {
            _context = context;
            _logger = logger; // Assign logger
        }

        // GET: api/Inventory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetInventories()
        {
            _logger.LogInformation("Fetching all inventories.");
            var inventories = await _context.Inventories.ToListAsync();
            return Ok(inventories);
        }

        // GET: api/Inventory/characterId/itemId
        [HttpGet("{characterId}/{itemId}")]
        public async Task<ActionResult<Inventory>> GetInventory(int characterId, int itemId)
        {
            _logger.LogInformation("Fetching inventory for CharacterId: {CharacterId} and ItemId: {ItemId}", characterId, itemId);

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.CharacterId == characterId && i.ItemId == itemId);

            if (inventory == null)
            {
                _logger.LogWarning("Inventory record not found for CharacterId: {CharacterId}, ItemId: {ItemId}", characterId, itemId);
                return NotFound();
            }

            return Ok(inventory);
        }

        // PUT: api/Inventory/characterId/itemId
        [HttpPut("{characterId}/{itemId}")]
        public async Task<IActionResult> PutInventory(int characterId, int itemId, Inventory inventory)
        {
            if (characterId != inventory.CharacterId || itemId != inventory.ItemId)
            {
                _logger.LogWarning("PUT request failed. Mismatched IDs: {CharacterId}/{ItemId} != {InventoryCharacterId}/{InventoryItemId}", characterId, itemId, inventory.CharacterId, inventory.ItemId);
                return BadRequest();
            }

            _context.Entry(inventory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Inventory updated for CharacterId: {CharacterId}, ItemId: {ItemId}", characterId, itemId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!InventoryExists(inventory.CharacterId, inventory.ItemId))
                {
                    _logger.LogWarning("Inventory update failed. Record not found for CharacterId: {CharacterId}, ItemId: {ItemId}", characterId, itemId);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error while updating inventory for CharacterId: {CharacterId}, ItemId: {ItemId}", characterId, itemId);
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Inventory
        [HttpPost]
        public async Task<ActionResult<Inventory>> PostInventory(Inventory inventory)
        {
            _logger.LogInformation("Adding new inventory record for CharacterId: {CharacterId}, ItemId: {ItemId}", inventory.CharacterId, inventory.ItemId);

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Inventory successfully added for CharacterId: {CharacterId}, ItemId: {ItemId}", inventory.CharacterId, inventory.ItemId);
            return CreatedAtAction(nameof(GetInventory), new { characterId = inventory.CharacterId, itemId = inventory.ItemId }, inventory);
        }

        // DELETE: api/Inventory/characterId/itemId
        [HttpDelete("{characterId}/{itemId}")]
        public async Task<IActionResult> DeleteInventory(int characterId, int itemId)
        {
            _logger.LogInformation("Attempting to delete inventory record for CharacterId: {CharacterId}, ItemId: {ItemId}", characterId, itemId);

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.CharacterId == characterId && i.ItemId == itemId);

            if (inventory == null)
            {
                _logger.LogWarning("Deletion failed. Inventory record not found for CharacterId: {CharacterId}, ItemId: {ItemId}", characterId, itemId);
                return NotFound();
            }

            _context.Inventories.Remove(inventory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Inventory record successfully deleted for CharacterId: {CharacterId}, ItemId: {ItemId}", characterId, itemId);
            return NoContent();
        }

        private bool InventoryExists(int characterId, int itemId)
        {
            return _context.Inventories.Any(e => e.CharacterId == characterId && e.ItemId == itemId);
        }
    }
}
