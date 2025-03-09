using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using ReemRPG.Controllers;
using ReemRPG.Models; 
using ReemRPG.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CharacterControllerTests
{
    private readonly Mock<ICharacterService> _characterServiceMock;
    private readonly Mock<ILogger<CharacterController>> _loggerMock;
    private readonly CharacterController _characterController;

    public CharacterControllerTests()
    {
        _characterServiceMock = new Mock<ICharacterService>(); // Fix field name
        _loggerMock = new Mock<ILogger<CharacterController>>(); // Mock logger

        // Pass the mocked service and logger to the controller
        _characterController = new CharacterController(_characterServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCharacters_ReturnsOkResult_WithCharacters()
    {
        // Arrange
        var characters = new List<Character>
        {
            new Character { CharacterId = 1, Name = "Archer", Class = "Ranger" }
        };

        _characterServiceMock.Setup(service => service.GetAllCharactersAsync()).ReturnsAsync(characters);

        // Act
        var result = await _characterController.GetCharacters();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCharacters = Assert.IsType<List<Character>>(okResult.Value);
        Assert.Single(returnedCharacters);
        Assert.Equal("Archer", returnedCharacters[0].Name);
    }

    [Fact]
    public async Task GetCharacter_CharacterExists_ReturnsOkResult()
    {
        // Arrange
        var character = new Character
        {
            CharacterId = 1,
            Name = "Mage",
            Class = "Wizard"  
        };

        _characterServiceMock.Setup(service => service.GetCharacterByIdAsync(1)).ReturnsAsync(character);

        // Act
        var result = await _characterController.GetCharacter(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCharacter = Assert.IsType<Character>(okResult.Value);
        Assert.Equal("Mage", returnedCharacter.Name);
    }

    [Fact]
    public async Task GetCharacter_CharacterDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _characterServiceMock.Setup(service => service.GetCharacterByIdAsync(99)).ReturnsAsync((Character)null);

        // Act
        var result = await _characterController.GetCharacter(99);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }
}
