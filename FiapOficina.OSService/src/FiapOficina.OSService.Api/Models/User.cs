namespace FiapOficina.OSService.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "Operator";
    public string Password { get; set; } = string.Empty;
}
