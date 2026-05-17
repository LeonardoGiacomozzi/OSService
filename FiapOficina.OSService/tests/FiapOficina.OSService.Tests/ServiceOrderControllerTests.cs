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
}
