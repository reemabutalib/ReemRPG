using System.ComponentModel.DataAnnotations;

namespace ReemRPG.DTOs
{
    public class CharacterDTO
    {
        // Character base data
        public int CharacterId { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public string ImageUrl { get; set; }

        // Base stats
        public int BaseStrength { get; set; }
        public int BaseAgility { get; set; }
        public int BaseIntelligence { get; set; }
        public int BaseHealth { get; set; }
        public int BaseAttackPower { get; set; }

        // Calculated stats
        public int Health { get; set; }
        public int AttackPower { get; set; }

        // User-specific progression
        public int Level { get; set; }
        public int Experience { get; set; }
        public int Gold { get; set; }

        // Flags
        public bool IsAssociatedWithUser { get; set; }
        public bool IsSelected { get; set; } // Adding the missing property
    }
}