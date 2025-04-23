using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReemRPG.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ReemRPG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController : ControllerBase
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<LeaderboardController> _logger;

        public LeaderboardController(ApplicationContext context, ILogger<LeaderboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Simple test endpoint that doesn't require database access
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "Leaderboard API is working properly" });
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaderboard([FromQuery] string sortBy = "experience")
        {
            try
            {
                sortBy = sortBy?.ToLower() ?? "experience";

                // Just make a simple query without complex joins
                var query = _context.Characters.AsQueryable();

                switch (sortBy)
                {
                    case "level":
                        query = query.OrderByDescending(c => c.Level);
                        break;
                    case "gold":
                        query = query.OrderByDescending(c => c.Gold);
                        break;
                    default:
                        query = query.OrderByDescending(c => c.Experience);
                        break;
                }

                var result = await query.Take(10)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        class_name = c.Class,
                        level = c.Level,
                        experience = c.Experience,
                        gold = c.Gold
                    })
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching leaderboard data");
                return StatusCode(500, "An error occurred while fetching leaderboard data");
            }
        }
    }
}