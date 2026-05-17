using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FiapOficina.OSService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly OSDbContext _context;
    private readonly IConfiguration _configuration;

    public UserController(OSDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "Username is required." });
        }

        // Tentar encontrar na tabela de Usuários do Sistema (Operadores)
        var systemUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());
        if (systemUser != null)
        {
            if (!string.IsNullOrEmpty(request.Password) && systemUser.Password != request.Password)
            {
                return Unauthorized(new { message = "Invalid password." });
            }

            var token = GenerateToken(systemUser.Username, systemUser.Role);
            return Ok(new { token });
        }

        return NotFound(new { message = "User not found." });
    }

    [HttpPost]
    [AllowAnonymous] // Permite criar usuários sem token (para o setup inicial)
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        if (string.IsNullOrWhiteSpace(user.Username))
        {
            return BadRequest(new { message = "Username is required." });
        }

        var exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == user.Username.ToLower());
        if (exists)
        {
            return BadRequest(new { message = "Username already exists." });
        }

        user.Id = Guid.NewGuid();
        if (string.IsNullOrWhiteSpace(user.Role))
        {
            user.Role = "Operator";
        }

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User created successfully", UserId = user.Id });
    }

    [HttpGet]
    [Authorize] // Apenas usuários autenticados podem listar operadores
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }


    private string GenerateToken(string username, string role)
    {
        var jwtKey = _configuration["JWT_KEY"] ?? "your-very-long-secret-key-shared-between-gateway-and-services";
        var jwtIssuer = _configuration["JWT_ISSUER"] ?? "fiap-oficina-auth";
        var jwtAudience = _configuration["JWT_AUDIENCE"] ?? "fiap-oficina-services";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("role", role)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
