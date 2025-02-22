using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class Item
{
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } // e.g., "Iron Sword"

    [Required]
    [StringLength(50, ErrorMessage = "Type must be at most 50 characters.")]
    public string Type { get; set; } // Weapon, Armor, Potion, Misc

    [Required]
    [StringLength(255, ErrorMessage = "Description must be at most 255 characters.")]
    public string Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Value must be a non-negative number.")]
    public int Value { get; set; } // How much it's worth (gold price)

    [Range(0, int.MaxValue, ErrorMessage = "Attack bonus must be a non-negative number.")]
    public int? AttackBonus { get; set; } // Extra attack power (if it's a weapon)

    [Range(0, int.MaxValue, ErrorMessage = "Defense bonus must be a non-negative number.")]
    public int? DefenseBonus { get; set; } // Extra defense (if it's armor)

    [Range(0, int.MaxValue, ErrorMessage = "Health restore value must be a non-negative number.")]
    public int? HealthRestore { get; set; } // Amount of HP restored (if it's a potion)

    // Many-to-Many Relationship: An item can be owned by many characters
    [JsonIgnore]
    public List<Inventory> Inventories { get; set; } = new List<Inventory>();
}
