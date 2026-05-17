using FiapOficina.Contracts;
using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace FiapOficina.OSService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceOrderController : ControllerBase
{
    private readonly IServiceOrderRepository _repository;
    private readonly IBus _bus;
    private readonly ILogger<ServiceOrderController> _logger;

    public ServiceOrderController(IServiceOrderRepository repository, IBus bus, ILogger<ServiceOrderController> logger)
    {
        _repository = repository;
        _bus = bus;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateServiceOrderDto dto)
    {
        var servicesTotal = dto.Services?.Sum(s => s.Value * s.Quantity) ?? 0;
        var materialsTotal = dto.Materials?.Sum(m => m.Value * m.Quantity) ?? 0;
        var totalValue = servicesTotal + materialsTotal;

        var order = new ServiceOrder
        {
            Id = Guid.NewGuid(),
            CustomerName = dto.Client?.Name ?? string.Empty,
            VehiclePlate = dto.Vehicle?.Plate ?? string.Empty,
            EstimatedValue = totalValue,
            Status = ServiceOrderStatus.Opened,
            CreatedOn = DateTime.UtcNow
        };

        _logger.LogInformation("Persistindo nova Ordem de Serviço aberta: {OrderId} para o cliente {CustomerName} no valor de {EstimatedValue}", order.Id, order.CustomerName, order.EstimatedValue);

        await _repository.AddAsync(order);

        return Ok(new { Message = "Ordem aberta com sucesso. Aguardando análise/diagnóstico.", OrderId = order.Id });
    }

    [HttpPost("{id}/analyze")]
    public async Task<IActionResult> AnalyzeOrder(Guid id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order == null) return NotFound(new { message = "Ordem de serviço não encontrada." });

        if (order.Status != ServiceOrderStatus.Opened)
        {
            return BadRequest(new { message = $"Apenas ordens no status 'Opened' podem ser analisadas. Status atual: {order.Status}" });
        }

        order.Status = ServiceOrderStatus.UnderAnalysis;
        await _repository.UpdateAsync(order);

        _logger.LogInformation("Operador iniciou diagnóstico/análise para OS {OrderId}.", id);

        return Ok(new { Message = "Análise iniciada com sucesso. A ordem de serviço está em diagnóstico.", OrderId = id });
    }

    [HttpPost("{id}/finish-analysis")]
    public async Task<IActionResult> FinishAnalysis(Guid id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order == null) return NotFound(new { message = "Ordem de serviço não encontrada." });

        if (order.Status != ServiceOrderStatus.UnderAnalysis)
        {
            return BadRequest(new { message = $"Apenas ordens no status 'UnderAnalysis' podem ter a análise finalizada. Status atual: {order.Status}" });
        }

        _logger.LogInformation("Operador finalizou o diagnóstico para OS {OrderId}. Publicando OrderOpened para gerar o orçamento...", id);

        // Envia o evento de abertura/diagnóstico para iniciar o orçamento na BillingService
        await _bus.Publish(new OrderOpened(order.Id, order.CustomerName, order.VehiclePlate, order.EstimatedValue));

        return Ok(new { Message = "Análise finalizada com sucesso e orçamento enviado para precificação.", OrderId = id });
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveOrder(Guid id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order == null) return NotFound(new { message = "Ordem de serviço não encontrada." });

        if (order.Status != ServiceOrderStatus.WaitingApproval)
        {
            return BadRequest(new { message = $"Apenas ordens no status 'WaitingApproval' podem ser aprovadas. Status atual: {order.Status}" });
        }

        order.Status = ServiceOrderStatus.Approved;
        order.ApprovedOn = DateTime.UtcNow;
        await _repository.UpdateAsync(order);

        _logger.LogInformation("Cliente aprovou o orçamento da OS {OrderId}. Publicando BudgetApproved...", id);

        // Envia o evento de orçamento aprovado para iniciar o pagamento via Mercado Pago
        await _bus.Publish(new BudgetApproved(order.Id, Guid.NewGuid(), order.EstimatedValue));

        return Ok(new { Message = "Orçamento aprovado com sucesso. Pagamento em processamento.", OrderId = id });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectOrder(Guid id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order == null) return NotFound(new { message = "Ordem de serviço não encontrada." });

        if (order.Status != ServiceOrderStatus.WaitingApproval)
        {
            return BadRequest(new { message = $"Apenas ordens no status 'WaitingApproval' podem ser rejeitadas. Status atual: {order.Status}" });
        }

        order.Status = ServiceOrderStatus.Rejected;
        await _repository.UpdateAsync(order);

        _logger.LogWarning("Cliente rejeitou o orçamento da OS {OrderId}. Cancelando ordem...", id);

        // Envia o evento de cancelamento para interromper o fluxo
        await _bus.Publish(new OrderCancelled(order.Id, "Orçamento rejeitado pelo cliente"));

        return Ok(new { Message = "Orçamento rejeitado e Ordem de Serviço cancelada.", OrderId = id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order == null) return NotFound();
        return Ok(order);
    }
}

public class CreateServiceOrderDto
{
    public ClientDto Client { get; set; } = new();
    public VehicleDto Vehicle { get; set; } = new();
    public List<ServiceDto> Services { get; set; } = new();
    public List<MaterialDto> Materials { get; set; } = new();
}

public class ClientDto
{
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class VehicleDto
{
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class ServiceDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Quantity { get; set; }
}

public class MaterialDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Quantity { get; set; }
}
