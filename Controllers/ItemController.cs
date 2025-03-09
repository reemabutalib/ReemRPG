using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ReemRPG.Models;
using ReemRPG.Services.Interfaces;

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly ILogger<ItemController> _logger; // Inject ILogger

        public ItemController(IItemService itemService, ILogger<ItemController> logger)
        {
            _itemService = itemService;
            _logger = logger; // Assign logger
        }

        // GET: api/Item
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems()
        {
            _logger.LogInformation("Fetching all items.");
            var items = await _itemService.GetAllItemsAsync();
            return Ok(items);
        }

        // GET: api/Item/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Item>> GetItem(int id)
        {
            _logger.LogInformation("Fetching item with ID: {ItemId}", id);

            var item = await _itemService.GetItemByIdAsync(id);
            if (item == null)
            {
                _logger.LogWarning("Item not found for ID: {ItemId}", id);
                return NotFound();
            }

            return Ok(item);
        }

        // PUT: api/Item/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutItem(int id, Item item)
        {
            if (id != item.Id)
            {
                _logger.LogWarning("PUT request failed. Mismatched IDs: {RequestId} != {ItemId}", id, item.Id);
                return BadRequest();
            }

            var updatedItem = await _itemService.UpdateItemAsync(id, item);
            if (updatedItem == null)
            {
                _logger.LogWarning("Update failed. Item not found for ID: {ItemId}", id);
                return NotFound();
            }

            _logger.LogInformation("Item updated successfully. ID: {ItemId}", id);
            return NoContent();
        }

        // POST: api/Item
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Item>> PostItem(Item item)
        {
            _logger.LogInformation("Creating new item: {ItemName}", item.Name);

            var newItem = await _itemService.CreateItemAsync(item);
            
            _logger.LogInformation("Item successfully created. ID: {ItemId}, Name: {ItemName}", newItem.Id, newItem.Name);
            return CreatedAtAction(nameof(GetItem), new { id = newItem.Id }, newItem);
        }

        // DELETE: api/Item/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            _logger.LogInformation("Attempting to delete item with ID: {ItemId}", id);

            var success = await _itemService.DeleteItemAsync(id);
            if (!success)
            {
                _logger.LogWarning("Deletion failed. Item not found for ID: {ItemId}", id);
                return NotFound();
            }

            _logger.LogInformation("Item successfully deleted. ID: {ItemId}", id);
            return NoContent();
        }
    }
}
