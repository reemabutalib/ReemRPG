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
    public Item? ItemReward { get; set; }  // nullable

    public List<CharacterQuest> CharacterQuests { get; set; } = new List<CharacterQuest>();
}