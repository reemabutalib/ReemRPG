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
    public class CharacterQuestController : ControllerBase
    {
        private readonly ApplicationContext _context;

        public CharacterQuestController(ApplicationContext context)
        {
            _context = context;
        }

        // GET: api/CharacterQuest
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CharacterQuest>>> GetCharacterQuests()
        {
            return await _context.CharacterQuests.ToListAsync();
        }

        // GET: api/CharacterQuest/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CharacterQuest>> GetCharacterQuest(int id)
        {
            var characterQuest = await _context.CharacterQuests.FindAsync(id);

            if (characterQuest == null)
            {
                return NotFound();
            }

            return characterQuest;
        }

        // PUT: api/CharacterQuest/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCharacterQuest(int id, CharacterQuest characterQuest)
        {
            if (id != characterQuest.Id)
            {
                return BadRequest();
            }

            _context.Entry(characterQuest).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CharacterQuestExists(id))
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

        // POST: api/CharacterQuest
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CharacterQuest>> PostCharacterQuest(CharacterQuest characterQuest)
        {
            _context.CharacterQuests.Add(characterQuest);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCharacterQuest", new { id = characterQuest.Id }, characterQuest);
        }

        // DELETE: api/CharacterQuest/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCharacterQuest(int id)
        {
            var characterQuest = await _context.CharacterQuests.FindAsync(id);
            if (characterQuest == null)
            {
                return NotFound();
            }

            _context.CharacterQuests.Remove(characterQuest);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CharacterQuestExists(int id)
        {
            return _context.CharacterQuests.Any(e => e.Id == id);
        }
    }
}
