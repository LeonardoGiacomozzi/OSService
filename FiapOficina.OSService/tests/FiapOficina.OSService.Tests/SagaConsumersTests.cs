using FiapOficina.Contracts;
using FiapOficina.OSService.Api.Consumers;
using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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
}
