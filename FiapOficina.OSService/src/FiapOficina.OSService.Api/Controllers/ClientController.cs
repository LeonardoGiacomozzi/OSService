using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapOficina.OSService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientController : ControllerBase
{
    private readonly IServiceOrderRepository _repository;

    public ClientController(IServiceOrderRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("{cpf}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetClientByCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return BadRequest(new { message = "CPF is required." });
        }

        var client = await _repository.GetClientByIdentifierAsync(cpf);
        if (client == null)
        {
            return NotFound(new { message = "Client not found." });
        }

        return Ok(client);
    }
}
