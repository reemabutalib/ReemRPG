using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http.Features;

public class Quest
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
    public int? ItemRewardId { get; set; } // nullable

    [ForeignKey("ItemRewardId")]
    // quest and item have a one-to-one relationship, a quest may reward one item and one item can belong to one quest
    public Item? ItemReward { get; set; }  // nullable
    
//one-to-many : quest can have multiple characterquests
    public List<CharacterQuest> CharacterQuests { get; set; } = new List<CharacterQuest>();
}