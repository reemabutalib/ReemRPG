using Moq;
using ReemRPG.Services.Interfaces;
using ReemRPG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using ReemRPG.Repositories.Interfaces;
using ReemRPG.Services;

public class CharacterServiceTests
{
    private readonly Mock<ICharacterRepository> _characterRepositoryMock;
    private readonly ICharacterService _characterService;

    public CharacterServiceTests()
    {
        _characterRepositoryMock = new Mock<ICharacterRepository>();
        _characterService = new CharacterService(_characterRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllCharactersAsync_ReturnsListOfCharacters()
    {
        // Arrange: Declare characters before using them in Setup
        var characters = new List<Character>
    {
        // creating mock characters 
        new Character { CharacterId = 1, Name = "Warrior", Class = "Fighter" },
        new Character { CharacterId = 2, Name = "Mage", Class = "Sorcerer" }
    };

        _characterRepositoryMock.Setup(repo => repo.GetAllCharactersAsync()).ReturnsAsync(characters);

        // Act
        var result = await _characterService.GetAllCharactersAsync();

        // Assert
        var characterList = result.ToList();
        Assert.Equal("Warrior", characterList[0].Name);
    }

    [Fact]
    public async Task GetCharacterByIdAsync_CharacterExists_ReturnsCharacter()
    {
        // Arrange: Declare character before using it in Setup
        var character = new Character { CharacterId = 1, Name = "Mage", Class = "Sorcerer" };

        _characterRepositoryMock.Setup(repo => repo.GetCharacterByIdAsync(1)).ReturnsAsync(character);

        // Act
        var result = await _characterService.GetCharacterByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Mage", result.Name);
    }

    [Fact]
    public async Task GetCharacterByIdAsync_CharacterDoesNotExist_ReturnsNull()
    {
        // Arrange
        _characterRepositoryMock.Setup(repo => repo.GetCharacterByIdAsync(It.IsAny<int>())).ReturnsAsync((Character)null);

        // Act
        var result = await _characterService.GetCharacterByIdAsync(99);

        // Assert
        Assert.Null(result);
    }
}
