using MassTransit;

namespace FiapOficina.OSService.Api.Models;

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; } // O OrderId será o CorrelationId
    public string CurrentState { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? BudgetedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public Guid? BudgetId { get; set; }
    public decimal Amount { get; set; }
}
