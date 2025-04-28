using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReemRPG.Models
{
    public class Quest
    {
        public Quest()
        {
            // Initialize collections
            CharacterQuests = new List<CharacterQuest>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public int RequiredLevel { get; set; } = 1;

        public int ExperienceReward { get; set; }

        public int GoldReward { get; set; }

        public int? ItemRewardId { get; set; }

        public Item ItemReward { get; set; }

        public bool Repeatable { get; set; } = false;

        // Add this navigation property for the many-to-many relationship
        public virtual ICollection<CharacterQuest> CharacterQuests { get; set; }
    }
}