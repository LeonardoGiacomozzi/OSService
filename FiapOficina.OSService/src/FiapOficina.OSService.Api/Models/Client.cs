using System.ComponentModel.DataAnnotations;

namespace FiapOficina.OSService.Api.Models;

public class Client
{
    [Key]
    public Guid Id { get; set; }
    public string Identifier { get; set; } = string.Empty; // CPF
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
