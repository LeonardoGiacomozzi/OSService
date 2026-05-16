using FiapOficina.Contracts;
using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using MassTransit;

namespace FiapOficina.OSService.Api.Consumers;

public class BudgetCreatedConsumer : IConsumer<BudgetCreated>
{
    private readonly IServiceOrderRepository _repository;
    private readonly ILogger<BudgetCreatedConsumer> _logger;

    public BudgetCreatedConsumer(IServiceOrderRepository repository, ILogger<BudgetCreatedConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BudgetCreated> context)
    {
        _logger.LogInformation("SAGA: Orçamento criado para OS {OrderId}", context.Message.OrderId);
        var order = await _repository.GetByIdAsync(context.Message.OrderId);
        if (order != null)
        {
            order.Status = ServiceOrderStatus.WaitingApproval;
            await _repository.UpdateAsync(order);
            _logger.LogInformation("Status da OS {OrderId} atualizado para WaitingApproval", context.Message.OrderId);
        }
    }
}

public class PaymentProcessedConsumer : IConsumer<PaymentProcessed>
{
    private readonly IServiceOrderRepository _repository;
    private readonly ILogger<PaymentProcessedConsumer> _logger;
    private readonly IBus _bus;

    public PaymentProcessedConsumer(IServiceOrderRepository repository, ILogger<PaymentProcessedConsumer> logger, IBus bus)
    {
        _repository = repository;
        _logger = logger;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<PaymentProcessed> context)
    {
        var order = await _repository.GetByIdAsync(context.Message.OrderId);
        if (order != null)
        {
            if (context.Message.Success)
            {
                order.Status = ServiceOrderStatus.Approved; // Pago = Aprovado para execução
                order.ApprovedOn = DateTime.UtcNow;
                _logger.LogInformation("SAGA: Pagamento aprovado para OS {OrderId}", context.Message.OrderId);
            }
            else
            {
                order.Status = ServiceOrderStatus.PaymentFailed;
                _logger.LogWarning("SAGA: Pagamento falhou para OS {OrderId}. Motivo: {Message}", context.Message.OrderId, context.Message.Message);
                
                // Compensação: Notifica cancelamento se necessário
                await _bus.Publish(new OrderCancelled(order.Id, $"Falha no pagamento: {context.Message.Message}"));
            }
            await _repository.UpdateAsync(order);
        }
    }
}

public class ExecutionFinishedConsumer : IConsumer<ExecutionFinished>
{
    private readonly IServiceOrderRepository _repository;
    private readonly ILogger<ExecutionFinishedConsumer> _logger;

    public ExecutionFinishedConsumer(IServiceOrderRepository repository, ILogger<ExecutionFinishedConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ExecutionFinished> context)
    {
        var order = await _repository.GetByIdAsync(context.Message.OrderId);
        if (order != null)
        {
            if (context.Message.Success)
            {
                order.Status = ServiceOrderStatus.Completed;
                _logger.LogInformation("SAGA: Execução concluída para OS {OrderId}", context.Message.OrderId);
            }
            else
            {
                order.Status = ServiceOrderStatus.ExecutionFailed;
                _logger.LogWarning("SAGA: Execução falhou para OS {OrderId}. Motivo: {Message}", context.Message.OrderId, context.Message.Message);
            }
            await _repository.UpdateAsync(order);
        }
    }
}

public class OrderCancelledConsumer : IConsumer<OrderCancelled>
{
    private readonly IServiceOrderRepository _repository;
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(IServiceOrderRepository repository, ILogger<OrderCancelledConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelled> context)
    {
        _logger.LogWarning("SAGA (Compensação): Cancelando OS {OrderId}. Motivo: {Reason}", context.Message.OrderId, context.Message.Reason);
        var order = await _repository.GetByIdAsync(context.Message.OrderId);
        if (order != null)
        {
            order.Status = ServiceOrderStatus.Cancelled;
            await _repository.UpdateAsync(order);
        }
    }
}
