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
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
    public async Task AnalyzeOrder_ShouldReturnOk_WhenOpened()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.Opened, CustomerName = "John", VehiclePlate = "Plate", EstimatedValue = 100 };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _controller.AnalyzeOrder(orderId);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);
        order.Status.Should().Be(ServiceOrderStatus.UnderAnalysis);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
        _busMock.Verify(b => b.Publish(It.IsAny<OrderOpened>(), default), Times.Never);
    }

    [Fact]
    public async Task AnalyzeOrder_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((ServiceOrder)null!);

        // Act
        var result = await _controller.AnalyzeOrder(orderId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AnalyzeOrder_ShouldReturnBadRequest_WhenNotOpened()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.WaitingApproval };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _controller.AnalyzeOrder(orderId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task FinishAnalysis_ShouldReturnOk_WhenUnderAnalysis()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.UnderAnalysis, CustomerName = "John", VehiclePlate = "Plate", EstimatedValue = 100 };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _controller.FinishAnalysis(orderId);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);
        _busMock.Verify(b => b.Publish(It.Is<OrderOpened>(e => e.OrderId == orderId), default), Times.Once);
    }

    [Fact]
    public async Task FinishAnalysis_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((ServiceOrder)null!);

        // Act
        var result = await _controller.FinishAnalysis(orderId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task FinishAnalysis_ShouldReturnBadRequest_WhenNotUnderAnalysis()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.Opened };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _controller.FinishAnalysis(orderId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ApproveOrder_ShouldReturnOk_WhenWaitingApproval()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.WaitingApproval, EstimatedValue = 150 };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _controller.ApproveOrder(orderId);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);
        order.Status.Should().Be(ServiceOrderStatus.Approved);
        order.ApprovedOn.Should().NotBeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
        _busMock.Verify(b => b.Publish(It.Is<BudgetApproved>(e => e.OrderId == orderId && e.Amount == 150), default), Times.Once);
    }

    [Fact]
    public async Task ApproveOrder_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((ServiceOrder)null!);

        // Act
        var result = await _controller.ApproveOrder(orderId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ApproveOrder_ShouldReturnBadRequest_WhenNotWaitingApproval()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.Opened };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _controller.ApproveOrder(orderId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RejectOrder_ShouldReturnOk_WhenWaitingApproval()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.WaitingApproval };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _controller.RejectOrder(orderId);

        // Assert
        var okResult = result.As<OkObjectResult>();
        okResult.StatusCode.Should().Be(200);
        order.Status.Should().Be(ServiceOrderStatus.Rejected);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
        _busMock.Verify(b => b.Publish(It.Is<OrderCancelled>(e => e.OrderId == orderId), default), Times.Once);
    }

    [Fact]
    public async Task RejectOrder_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((ServiceOrder)null!);

        // Act
        var result = await _controller.RejectOrder(orderId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RejectOrder_ShouldReturnBadRequest_WhenNotWaitingApproval()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.Opened };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _controller.RejectOrder(orderId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
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
