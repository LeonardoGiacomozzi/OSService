using FiapOficina.OSService.Api.Controllers;
using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FiapOficina.OSService.Tests;

public class UserControllerTests
{
    private readonly OSDbContext _context;
    private readonly Mock<IConfiguration> _configurationMock;

    public UserControllerTests()
    {
        var options = new DbContextOptionsBuilder<OSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OSDbContext(options);
        _configurationMock = new Mock<IConfiguration>();

        // Configuração de chaves JWT para os testes passarem sem lançar erro
        _configurationMock.Setup(c => c["JWT_KEY"]).Returns("this-is-a-very-long-test-jwt-secret-key-designed-specifically-for-unit-testing");
        _configurationMock.Setup(c => c["JWT_ISSUER"]).Returns("fiap-oficina-auth");
        _configurationMock.Setup(c => c["JWT_AUDIENCE"]).Returns("fiap-oficina-services");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var controller = new UserController(_context, _configurationMock.Object);
        var user = new User
        {
            Username = "operator1",
            Name = "Operator One",
            Password = "pass",
            Role = "Operator"
        };

        // Act
        var result = await controller.CreateUser(user);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);

        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "operator1");
        savedUser.Should().NotBeNull();
        savedUser!.Name.Should().Be("Operator One");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenUsernameIsEmpty()
    {
        // Arrange
        var controller = new UserController(_context, _configurationMock.Object);
        var user = new User { Username = "" };

        // Act
        var result = await controller.CreateUser(user);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenUsernameAlreadyExists()
    {
        // Arrange
        _context.Users.Add(new User { Id = Guid.NewGuid(), Username = "duplicate", Name = "First" });
        await _context.SaveChangesAsync();

        var controller = new UserController(_context, _configurationMock.Object);
        var user = new User { Username = "duplicate", Name = "Second" };

        // Act
        var result = await controller.CreateUser(user);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "loginuser",
            Name = "Login User",
            Password = "correctpassword",
            Role = "Operator"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var controller = new UserController(_context, _configurationMock.Object);
        var request = new LoginRequest { Username = "loginuser", Password = "correctpassword" };

        // Act
        var result = await controller.Login(request);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);
        
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenUsernameIsEmpty()
    {
        // Arrange
        var controller = new UserController(_context, _configurationMock.Object);
        var request = new LoginRequest { Username = "" };

        // Act
        var result = await controller.Login(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsIncorrect()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "loginuser2",
            Name = "Login User 2",
            Password = "correctpassword",
            Role = "Operator"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var controller = new UserController(_context, _configurationMock.Object);
        var request = new LoginRequest { Username = "loginuser2", Password = "wrongpassword" };

        // Act
        var result = await controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var controller = new UserController(_context, _configurationMock.Object);
        var request = new LoginRequest { Username = "nonexistent", Password = "any" };

        // Act
        var result = await controller.Login(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUsers_ShouldReturnOk()
    {
        // Arrange
        _context.Users.Add(new User { Id = Guid.NewGuid(), Username = "user1", Name = "User One" });
        _context.Users.Add(new User { Id = Guid.NewGuid(), Username = "user2", Name = "User Two" });
        await _context.SaveChangesAsync();

        var controller = new UserController(_context, _configurationMock.Object);

        // Act
        var result = await controller.GetUsers();

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);

        var list = okResult.Value.As<List<User>>();
        list.Count.Should().BeGreaterThanOrEqualTo(2);
    }
}
