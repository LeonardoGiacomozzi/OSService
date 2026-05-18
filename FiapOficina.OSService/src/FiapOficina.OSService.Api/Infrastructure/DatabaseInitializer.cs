using Microsoft.EntityFrameworkCore;
using FiapOficina.OSService.Api.Models;

namespace FiapOficina.OSService.Api.Infrastructure;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class DatabaseInitializer
{
    public static void InitializeDatabase(OSDbContext dbContext)
    {
        dbContext.Database.Migrate();

        if (!dbContext.Users.Any())
        {
            dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Name = "System Operator",
                Role = "Operator",
                Password = "admin" // NOSONAR
            });
            dbContext.SaveChanges();
        }

        if (!dbContext.Clients.Any())
        {
            dbContext.Clients.Add(new Client
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"),
                Identifier = "12345678909",
                Name = "Cliente Padrao Oficina"
            });
            dbContext.SaveChanges();
        }
    }
}
