using FiapOficina.OSService.Api.Models;
using FluentAssertions;
using Xunit;

namespace FiapOficina.OSService.Tests;

public class ModelTests
{
    [Fact]
    public void ServiceOrder_ShouldStoreDataCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var approvedOn = DateTime.UtcNow.AddHours(1);
        var finishedOn = DateTime.UtcNow.AddHours(2);
        var createdOn = DateTime.UtcNow;

        // Act
        var order = new ServiceOrder
        {
            Id = id,
            VehicleId = vehicleId,
            CustomerName = "John",
            VehiclePlate = "ABC",
            EstimatedValue = 100,
            Status = ServiceOrderStatus.Opened,
            CreatedOn = createdOn,
            ApprovedOn = approvedOn,
            FinishedOn = finishedOn
        };

        // Assert
        order.Id.Should().Be(id);
        order.VehicleId.Should().Be(vehicleId);
        order.CustomerName.Should().Be("John");
        order.VehiclePlate.Should().Be("ABC");
        order.EstimatedValue.Should().Be(100);
        order.Status.Should().Be(ServiceOrderStatus.Opened);
        order.CreatedOn.Should().Be(createdOn);
        order.ApprovedOn.Should().Be(approvedOn);
        order.FinishedOn.Should().Be(finishedOn);
    }

    [Fact]
    public void OrderState_ShouldStoreDataCorrectly()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var budgetedAt = DateTime.UtcNow.AddMinutes(5);
        var approvedAt = DateTime.UtcNow.AddMinutes(10);
        var paidAt = DateTime.UtcNow.AddMinutes(15);
        var finishedAt = DateTime.UtcNow.AddMinutes(20);

        // Act
        var state = new OrderState
        {
            CorrelationId = correlationId,
            CurrentState = "Processing",
            CreatedAt = createdAt,
            BudgetedAt = budgetedAt,
            ApprovedAt = approvedAt,
            PaidAt = paidAt,
            FinishedAt = finishedAt,
            BudgetId = budgetId,
            Amount = 500
        };

        // Assert
        state.CorrelationId.Should().Be(correlationId);
        state.CurrentState.Should().Be("Processing");
        state.CreatedAt.Should().Be(createdAt);
        state.BudgetedAt.Should().Be(budgetedAt);
        state.ApprovedAt.Should().Be(approvedAt);
        state.PaidAt.Should().Be(paidAt);
        state.FinishedAt.Should().Be(finishedAt);
        state.BudgetId.Should().Be(budgetId);
        state.Amount.Should().Be(500);
    }
}
