using FiapOficina.OSService.Api.Models;
using FluentAssertions;
using Xunit;

namespace FiapOficina.OSService.Tests;

public class ModelTests
{
    [Fact]
    public void ServiceOrder_ShouldStoreDataCorrectly()
    {
        // Arrange & Act
        var order = new ServiceOrder
        {
            Id = Guid.NewGuid(),
            CustomerName = "John",
            VehiclePlate = "ABC",
            EstimatedValue = 100,
            Status = ServiceOrderStatus.Opened,
            CreatedOn = DateTime.UtcNow
        };

        // Assert
        order.CustomerName.Should().Be("John");
        order.VehiclePlate.Should().Be("ABC");
        order.EstimatedValue.Should().Be(100);
    }

    [Fact]
    public void OrderState_ShouldStoreDataCorrectly()
    {
        // Arrange & Act
        var state = new OrderState
        {
            CorrelationId = Guid.NewGuid(),
            CurrentState = "Processing",
            CreatedAt = DateTime.UtcNow,
            Amount = 500
        };

        // Assert
        state.CurrentState.Should().Be("Processing");
        state.Amount.Should().Be(500);
    }
}
