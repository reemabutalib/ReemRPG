using ReemRPG.Models;
using Microsoft.AspNetCore.Identity;
public class UserCharacter
{
    public string UserId { get; set; }
    public int CharacterId { get; set; }

    // User-specific progress data
    public int Experience { get; set; } = 0;
    public int Level { get; set; } = 1;
    public int Gold { get; set; } = 0;

    // Navigation properties
    public Character Character { get; set; }
    public IdentityUser User { get; set; }
}