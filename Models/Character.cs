using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace ReemRPG.Models
{
    public class Character
    {
        public Character()
        {
            // Initialize collections
            CharacterQuests = new List<CharacterQuest>();
        }

        [Key]
        public int CharacterId { get; set; }

        // Base character definition (not user-specific)
        public string Name { get; set; }
        public string Class { get; set; }
        public string ImageUrl { get; set; }

        // Base stats
        public int BaseStrength { get; set; }
        public int BaseAgility { get; set; }
        public int BaseIntelligence { get; set; }

        // Additional base stats
        public int BaseHealth { get; set; } = 100;
        public int BaseAttackPower { get; set; } = 10;

        // Collections
        public virtual ICollection<CharacterQuest> CharacterQuests { get; set; }

    }
}