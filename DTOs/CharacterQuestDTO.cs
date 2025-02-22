using System.ComponentModel.DataAnnotations;
namespace ReemRPG.DTOs
{
public class CharacterQuestDTO
{
    public int CharacterId { get; set; }
    public int QuestId { get; set; }
    public bool IsCompleted { get; set; }
    }
}