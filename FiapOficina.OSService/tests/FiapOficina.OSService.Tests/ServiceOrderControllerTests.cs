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
        var serviceOrder = new ServiceOrder
        {
            CustomerName = "John Doe",
            VehiclePlate = "ABC-1234",
            EstimatedValue = 500
        };

        // Act
        var result = await _controller.CreateOrder(serviceOrder);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);
        
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ServiceOrder>()), Times.Once);
        _busMock.Verify(b => b.Publish(It.IsAny<OrderOpened>(), default), Times.Once);
    }
}
