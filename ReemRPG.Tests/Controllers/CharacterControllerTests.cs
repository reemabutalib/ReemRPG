using Moq;
using Microsoft.AspNetCore.Mvc;
using ReemRPG.Controllers;
using ReemRPG.Services.Interfaces;
using ReemRPG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class CharacterControllerTests
{
    private readonly Mock<ICharacterService> _characterServiceMock;
    private readonly CharacterController _controller;

    public CharacterControllerTests()
    {
        _characterServiceMock = new Mock<ICharacterService>();
        _controller = new CharacterController(_characterServiceMock.Object);
    }

    [Fact]
public async Task GetCharacters_ReturnsOkResult_WithCharacters()
{
    var characters = new List<Character>
    {
        new Character { CharacterId = 1, Name = "Archer", Class = "Ranger" }
    };

    // setup matches the method's return type (Task<List<Character>>)
    _characterServiceMock.Setup(service => service.GetAllCharactersAsync()).ReturnsAsync(characters);

    // Act
    var result = await _controller.GetCharacters();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result.Result);
    var returnedCharacters = Assert.IsType<List<Character>>(okResult.Value);
    Assert.Single(returnedCharacters);
    Assert.Equal("Archer", returnedCharacters[0].Name);
}


    [Fact]
    public async Task GetCharacter_CharacterExists_ReturnsOkResult()
    {

        var character = new Character
        {
            CharacterId = 1,
            Name = "Mage",
            Class = "Wizard"  
        };

        _characterServiceMock.Setup(service => service.GetCharacterByIdAsync(1)).ReturnsAsync(character);

        // Act
        var result = await _controller.GetCharacter(1);

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
        var result = await _controller.GetCharacter(99);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }
}
