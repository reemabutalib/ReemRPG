using System.ComponentModel.DataAnnotations;
namespace ReemRPG.DTOs
{
public class QuestDTO
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
    public int? ItemRewardId { get; set; }
    } 
}