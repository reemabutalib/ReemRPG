using System.ComponentModel.DataAnnotations;

namespace ReemRPG.DTOs
{
    public class CharacterDTO
    {
        public int CharacterId { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
        public required string Name { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Class name cannot exceed 20 characters.")]
        public required string Class { get; set; }

        [Range(1, 100, ErrorMessage = "Level must be between 1 and 100.")]
        public int Level { get; set; } = 1;

        [Range(1, 1000, ErrorMessage = "Health must be between 1 and 1000.")]
        public int Health { get; set; } = 100;

        [Range(1, 500, ErrorMessage = "Attack Power must be between 1 and 500.")]
        public int AttackPower { get; set; } = 10;
    }
}
