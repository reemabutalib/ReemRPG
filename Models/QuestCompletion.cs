using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReemRPG.Models
{
    public class QuestCompletion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuestId { get; set; }

        [Required]
        public int CharacterId { get; set; }

        [ForeignKey("CharacterId")]
        public Character Character { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public DateTime CompletedOn { get; set; } = DateTime.UtcNow;

        public int ExperienceGained { get; set; }

        public int GoldGained { get; set; }
    }
}