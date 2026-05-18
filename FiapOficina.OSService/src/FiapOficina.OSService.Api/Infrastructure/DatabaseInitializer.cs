using Microsoft.EntityFrameworkCore;
using FiapOficina.OSService.Api.Models;

namespace FiapOficina.OSService.Api.Infrastructure;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class DatabaseInitializer
{
    public static void InitializeDatabase(OSDbContext dbContext)
    {
        try
        {
            dbContext.Database.Migrate();
        }
        catch (Exception ex) when (ex.Message.Contains("42P07") || ex.Message.Contains("already exists"))
        {
            Console.WriteLine("[DatabaseInitializer] As tabelas já existem no banco da AWS. Inserindo migração inicial na tabela de histórico para sincronizar o EF Core.");
            try
            {
                dbContext.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                        ""MigrationId"" character varying(150) NOT NULL,
                        ""ProductVersion"" character varying(32) NOT NULL,
                        CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                    );
                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                    VALUES ('20260518215325_InitialCreate', '10.0.0')
                    ON CONFLICT DO NOTHING;
                ");
                dbContext.Database.Migrate();
            }
            catch (Exception retryEx)
            {
                Console.WriteLine($"[DatabaseInitializer] Falha ao registrar migração inicial no histórico: {retryEx.Message}");
                throw;
            }
        }

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
