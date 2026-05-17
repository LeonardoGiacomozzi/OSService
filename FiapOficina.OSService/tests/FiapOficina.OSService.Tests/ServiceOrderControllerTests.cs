using FiapOficina.Contracts;
using FiapOficina.OSService.Api.Controllers;
using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Collections.Generic;

namespace FiapOficina.OSService.Tests;

public class ServiceOrderControllerTests
{
    private readonly Mock<IServiceOrderRepository> _repositoryMock;
    private readonly Mock<IBus> _busMock;
    private readonly Mock<ILogger<ServiceOrderController>> _loggerMock;
    private readonly ServiceOrderController _controller;

    public ServiceOrderControllerTests()
    {
        _repositoryMock = new Mock<IServiceOrderRepository>();
        _busMock = new Mock<IBus>();
        _loggerMock = new Mock<ILogger<ServiceOrderController>>();
        _controller = new ServiceOrderController(_repositoryMock.Object, _busMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var dto = new CreateServiceOrderDto
        {
            Client = new ClientDto { Name = "John Doe" },
            Vehicle = new VehicleDto { Plate = "ABC-1234" },
            Services = new List<ServiceDto>
            {
                new ServiceDto { Value = 100, Quantity = 5 }
            }
        };

        // Act
        var result = await _controller.CreateOrder(dto);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);
        
        _repositoryMock.Verify(r => r.AddAsync(It.Is<ServiceOrder>(o => o.CustomerName == "John Doe" && o.VehiclePlate == "ABC-1234" && o.EstimatedValue == 500)), Times.Once);
        _busMock.Verify(b => b.Publish(It.Is<OrderOpened>(e => e.CustomerName == "John Doe" && e.VehiclePlate == "ABC-1234" && e.EstimatedValue == 500), default), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnOk_WhenDtoHasNullListsAndNestedProperties()
    {
        // Arrange
        var dto = new CreateServiceOrderDto
        {
            Client = null!,
            Vehicle = null!,
            Services = null!,
            Materials = null!
        };

        // Act
        var result = await _controller.CreateOrder(dto);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<ServiceOrder>(o => o.CustomerName == string.Empty && o.VehiclePlate == string.Empty && o.EstimatedValue == 0)), Times.Once);
        _busMock.Verify(b => b.Publish(It.Is<OrderOpened>(e => e.CustomerName == string.Empty && e.VehiclePlate == string.Empty && e.EstimatedValue == 0), default), Times.Once);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnOk_WhenOrderExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, CustomerName = "Test" };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(order);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((ServiceOrder)null!);

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void DtoClasses_ShouldGetAndSetPropertiesCorrectly()
    {
        // Arrange & Act
        var client = new ClientDto
        {
            Name = "Leonardo",
            Identifier = "123",
            Phone = "456",
            Email = "test@test.com",
            Address = "Rua Test"
        };

        var vehicle = new VehicleDto
        {
            Brand = "Jeep",
            Model = "Compass",
            Year = 2021,
            Plate = "QHJ9B01",
            Color = "Grey"
        };

        var service = new ServiceDto
        {
            Name = "Service",
            Description = "Desc",
            Value = 10,
            Quantity = 4
        };

        var material = new MaterialDto
        {
            Name = "Material",
            Description = "Desc",
            Brand = "Brand",
            Value = 2,
            Quantity = 4
        };

        var dto = new CreateServiceOrderDto
        {
            Client = client,
            Vehicle = vehicle,
            Services = new List<ServiceDto> { service },
            Materials = new List<MaterialDto> { material }
        };

        // Assert
        dto.Client.Name.Should().Be("Leonardo");
        dto.Client.Identifier.Should().Be("123");
        dto.Client.Phone.Should().Be("456");
        dto.Client.Email.Should().Be("test@test.com");
        dto.Client.Address.Should().Be("Rua Test");

        dto.Vehicle.Brand.Should().Be("Jeep");
        dto.Vehicle.Model.Should().Be("Compass");
        dto.Vehicle.Year.Should().Be(2021);
        dto.Vehicle.Plate.Should().Be("QHJ9B01");
        dto.Vehicle.Color.Should().Be("Grey");

        dto.Services[0].Name.Should().Be("Service");
        dto.Services[0].Description.Should().Be("Desc");
        dto.Services[0].Value.Should().Be(10);
        dto.Services[0].Quantity.Should().Be(4);

        dto.Materials[0].Name.Should().Be("Material");
        dto.Materials[0].Description.Should().Be("Desc");
        dto.Materials[0].Brand.Should().Be("Brand");
        dto.Materials[0].Value.Should().Be(2);
        dto.Materials[0].Quantity.Should().Be(4);
    }
}
