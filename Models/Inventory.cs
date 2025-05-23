using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ReemRPG.Models;

public class Inventory
{
    [Key]
    [Column(Order = 0)]  
    public int CharacterId { get; set; }

    [Key]
    [Column(Order = 1)]
    public int ItemId { get; set; }

    // Navigation properties
    [JsonIgnore] // Prevents infinite loops when serializing JSON

    // many-to-many relationship with Character and Item. Inventory table is the join table to map multiple relations. CharacterID and ItemID form a composite primary key.
    public Character? Character { get; set; }

    [JsonIgnore]
    public Item? Item { get; set; }
}
