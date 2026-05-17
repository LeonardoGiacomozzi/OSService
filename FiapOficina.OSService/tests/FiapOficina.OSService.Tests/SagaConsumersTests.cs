using FiapOficina.Contracts;
using FiapOficina.OSService.Api.Consumers;
using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace FiapOficina.OSService.Tests;

public class SagaConsumersTests
{
    private readonly Mock<IServiceOrderRepository> _repositoryMock;
    private readonly Mock<ILogger<BudgetCreatedConsumer>> _budgetLoggerMock;
    private readonly Mock<ILogger<PaymentProcessedConsumer>> _paymentLoggerMock;
    private readonly Mock<IBus> _busMock;

    public SagaConsumersTests()
    {
        _repositoryMock = new Mock<IServiceOrderRepository>();
        _budgetLoggerMock = new Mock<ILogger<BudgetCreatedConsumer>>();
        _paymentLoggerMock = new Mock<ILogger<PaymentProcessedConsumer>>();
        _busMock = new Mock<IBus>();
    }

    [Fact]
    public async Task BudgetCreatedConsumer_ShouldUpdateStatusToWaitingApproval()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.Opened };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        var consumer = new BudgetCreatedConsumer(_repositoryMock.Object, _budgetLoggerMock.Object);
        var contextMock = new Mock<ConsumeContext<BudgetCreated>>();
        contextMock.Setup(c => c.Message).Returns(new BudgetCreated(orderId, Guid.NewGuid(), 500));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        order.Status.Should().Be(ServiceOrderStatus.WaitingApproval);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task PaymentProcessedConsumer_ShouldUpdateStatusToApproved_WhenSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.WaitingApproval };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        var consumer = new PaymentProcessedConsumer(_repositoryMock.Object, _paymentLoggerMock.Object, _busMock.Object);
        var contextMock = new Mock<ConsumeContext<PaymentProcessed>>();
        contextMock.Setup(c => c.Message).Returns(new PaymentProcessed(orderId, Guid.NewGuid(), true, "Success"));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        order.Status.Should().Be(ServiceOrderStatus.Approved);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task PaymentProcessedConsumer_ShouldUpdateStatusToPaymentFailed_AndPublishCancel_WhenFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.WaitingApproval };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        var consumer = new PaymentProcessedConsumer(_repositoryMock.Object, _paymentLoggerMock.Object, _busMock.Object);
        var contextMock = new Mock<ConsumeContext<PaymentProcessed>>();
        contextMock.Setup(c => c.Message).Returns(new PaymentProcessed(orderId, Guid.NewGuid(), false, "Refused"));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        order.Status.Should().Be(ServiceOrderStatus.PaymentFailed);
        _busMock.Verify(b => b.Publish(It.IsAny<OrderCancelled>(), default), Times.Once);
    }

    [Fact]
    public async Task ExecutionFinishedConsumer_ShouldUpdateStatusToCompleted_WhenSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.Approved };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        var consumer = new ExecutionFinishedConsumer(_repositoryMock.Object, new Mock<ILogger<ExecutionFinishedConsumer>>().Object);
        var contextMock = new Mock<ConsumeContext<ExecutionFinished>>();
        contextMock.Setup(c => c.Message).Returns(new ExecutionFinished(orderId, true, "Done"));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        order.Status.Should().Be(ServiceOrderStatus.Completed);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task ExecutionFinishedConsumer_ShouldUpdateStatusToExecutionFailed_WhenFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.Approved };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        var consumer = new ExecutionFinishedConsumer(_repositoryMock.Object, new Mock<ILogger<ExecutionFinishedConsumer>>().Object);
        var contextMock = new Mock<ConsumeContext<ExecutionFinished>>();
        contextMock.Setup(c => c.Message).Returns(new ExecutionFinished(orderId, false, "Execution failed due to missing parts"));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        order.Status.Should().Be(ServiceOrderStatus.ExecutionFailed);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task OrderCancelledConsumer_ShouldUpdateStatusToCancelled()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new ServiceOrder { Id = orderId, Status = ServiceOrderStatus.Opened };
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        var consumer = new OrderCancelledConsumer(_repositoryMock.Object, new Mock<ILogger<OrderCancelledConsumer>>().Object);
        var contextMock = new Mock<ConsumeContext<OrderCancelled>>();
        contextMock.Setup(c => c.Message).Returns(new OrderCancelled(orderId, "Test Reason"));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        order.Status.Should().Be(ServiceOrderStatus.Cancelled);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task Consumers_ShouldNotUpdate_WhenOrderNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((ServiceOrder)null!);

        var budgetConsumer = new BudgetCreatedConsumer(_repositoryMock.Object, _budgetLoggerMock.Object);
        var contextMock = new Mock<ConsumeContext<BudgetCreated>>();
        contextMock.Setup(c => c.Message).Returns(new BudgetCreated(orderId, Guid.NewGuid(), 100));

        // Act
        await budgetConsumer.Consume(contextMock.Object);

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ServiceOrder>()), Times.Never);
    }
}
