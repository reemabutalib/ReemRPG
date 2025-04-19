namespace ReemRPG.Models
{
    public class UserCharacter
    {
        public string UserId { get; set; } // ID of the user
        public int CharacterId { get; set; } // ID of the selected character

        // Navigation properties for relationships
        public Character Character { get; set; }
    }
}