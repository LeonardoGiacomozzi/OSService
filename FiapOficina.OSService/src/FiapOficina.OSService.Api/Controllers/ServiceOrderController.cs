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

        _logger.LogInformation("Persistindo e publicando nova Ordem de Serviço: {OrderId} para o cliente {CustomerName} no valor de {EstimatedValue}", order.Id, order.CustomerName, order.EstimatedValue);

        await _repository.AddAsync(order);

        // Notifica o sistema que uma nova OS foi aberta (inicia a SAGA)
        await _bus.Publish(new OrderOpened(order.Id, order.CustomerName, order.VehiclePlate, order.EstimatedValue));

        return Ok(new { Message = "Ordem aberta com sucesso", OrderId = order.Id });
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
