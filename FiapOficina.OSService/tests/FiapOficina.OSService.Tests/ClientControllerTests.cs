using FiapOficina.OSService.Api.Controllers;
using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

namespace FiapOficina.OSService.Tests;

public class ClientControllerTests
{
    private readonly Mock<IServiceOrderRepository> _repositoryMock;
    private readonly ClientController _controller;

    public ClientControllerTests()
    {
        _repositoryMock = new Mock<IServiceOrderRepository>();
        _controller = new ClientController(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetClientByCpf_ShouldReturnBadRequest_WhenCpfIsEmptyOrWhitespace()
    {
        // Act
        var result = await _controller.GetClientByCpf("   ");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetClientByCpf_ShouldReturnNotFound_WhenClientDoesNotExist()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetClientByIdentifierAsync("12345678909"))
                       .ReturnsAsync((Client)null!);

        // Act
        var result = await _controller.GetClientByCpf("12345678909");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetClientByCpf_ShouldReturnOkWithClient_WhenClientExists()
    {
        // Arrange
        var client = new Client
        {
            Id = Guid.NewGuid(),
            Identifier = "12345678909",
            Name = "John Doe",
            Phone = "12345",
            Email = "john@doe.com",
            Address = "Street 1"
        };
        _repositoryMock.Setup(r => r.GetClientByIdentifierAsync("12345678909"))
                       .ReturnsAsync(client);

        // Act
        var result = await _controller.GetClientByCpf("12345678909");

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);

        var returnedClient = okResult.Value.As<Client>();
        returnedClient.Should().NotBeNull();
        returnedClient.Identifier.Should().Be("12345678909");
        returnedClient.Name.Should().Be("John Doe");
    }
}
