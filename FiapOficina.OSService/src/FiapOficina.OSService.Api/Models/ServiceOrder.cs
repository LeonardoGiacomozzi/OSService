using System.ComponentModel.DataAnnotations;

namespace FiapOficina.OSService.Api.Models;

public enum ServiceOrderStatus
{
    Opened = 1,
    WaitingApproval = 2,
    Approved = 3,
    Rejected = 4,
    InProgress = 5,
    Completed = 6,
    Delivered = 7,
    Cancelled = 8,
    PaymentFailed = 9,
    ExecutionFailed = 10,
    UnderAnalysis = 11
}

public class ServiceOrder
{
    [Key]
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public decimal EstimatedValue { get; set; }
    public ServiceOrderStatus Status { get; set; } = ServiceOrderStatus.Opened;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedOn { get; set; }
    public DateTime? FinishedOn { get; set; }
}
