using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReemRPG.Models;
using ReemRPG.Services.Interfaces;

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly IItemService _itemService;

        public ItemController(IItemService itemService)
        {
            _itemService = itemService;
        }

        // GET: api/Item
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems()
        {
            return Ok(await _itemService.GetAllItemsAsync());
        }

        // GET: api/Item/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Item>> GetItem(int id)
        {
            var item = await _itemService.GetItemByIdAsync(id);
            if (item == null)
                return NotFound();
            return Ok(item);
        }

        // PUT: api/Item/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutItem(int id, Item item)
        {
            if (id != item.Id)
                return BadRequest();

            var updatedItem = await _itemService.UpdateItemAsync(id, item);
            if (updatedItem == null)
                return NotFound();

            return NoContent();
        }

        // POST: api/Item
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Item>> PostItem(Item item)
        {
            var newItem = await _itemService.CreateItemAsync(item);
            return CreatedAtAction(nameof(GetItem), new { id = newItem.Id }, newItem);
        }

        // DELETE: api/Item/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var success = await _itemService.DeleteItemAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
