using System.ComponentModel.DataAnnotations;
namespace ReemRPG.DTOs
{
    public class ItemDTO
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Type must be at most 50 characters.")]
        public string Type { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "Description must be at most 255 characters.")]
        public string Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Value must be a non-negative number.")]
        public int Value { get; set; }
    }
}
