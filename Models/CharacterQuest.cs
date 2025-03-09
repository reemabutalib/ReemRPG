using System;
using System.Text.Json.Serialization;
using ReemRPG.Models;

public class CharacterQuest
{
    // Ensure there is a primary key
    public int Id { get; set; }

    public int CharacterId { get; set; }
    public Character? Character { get; set; }
// many-to-many relationship between character and quest
    public int QuestId { get; set; }
    public Quest? Quest { get; set; }

    public bool IsCompleted { get; set; } = false;

    public DateTime? CompletionDate { get; set; } // Nullable to allow unfinished quests
}


