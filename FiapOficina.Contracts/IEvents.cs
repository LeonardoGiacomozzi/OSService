namespace FiapOficina.Contracts;

// Events (Pub/Sub)
public record OrderOpened(Guid OrderId, string CustomerName, string VehiclePlate, decimal EstimatedValue);
public record BudgetCreated(Guid OrderId, Guid BudgetId, decimal TotalAmount);
public record BudgetApproved(Guid OrderId, Guid BudgetId, decimal Amount);
public record PaymentProcessed(Guid OrderId, Guid PaymentId, bool Success, string Message);
public record ExecutionStarted(Guid OrderId);
public record ExecutionFinished(Guid OrderId, bool Success, string? Message);
public record OrderFinalized(Guid OrderId);

// Compensating Events (Rollback)
public record OrderCancelled(Guid OrderId, string Reason);
public record PaymentRefunded(Guid OrderId, Guid PaymentId);

// Commands (Point-to-Point from SAGA if using Orchestration, 
// but we are using Coreography)
public record CreateBudgetCommand(Guid OrderId, decimal EstimatedValue);
public record ProcessPaymentCommand(Guid OrderId, Guid BudgetId, decimal Amount);
public record StartExecutionCommand(Guid OrderId);
