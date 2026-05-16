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
    public async Task<IActionResult> CreateOrder([FromBody] ServiceOrder order)
    {
        order.Id = Guid.NewGuid();
        order.Status = ServiceOrderStatus.Opened;
        order.CreatedOn = DateTime.UtcNow;

        _logger.LogInformation("Persistindo e publicando nova Ordem de Serviço: {OrderId}", order.Id);

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
