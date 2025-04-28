namespace ReemRPG.Models
{
    // This class is used to represent a request to select a character, purpose is to solve the issue of
    // different naming conventions between C# and JavaScript.
    public class SelectCharacterRequest
    {
        // Property with PascalCase (for C# binding)
        public int CharacterId { get; set; }

        // Property with camelCase (for JavaScript binding)
        public int characterId { get; set; }
    }
}