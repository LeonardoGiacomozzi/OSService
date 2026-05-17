using FiapOficina.OSService.Api.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FiapOficina.OSService.Api.Infrastructure;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class OSDbContext : DbContext
{
    public OSDbContext(DbContextOptions<OSDbContext> options) : base(options) { }

    public DbSet<ServiceOrder> ServiceOrders { get; set; }
    public DbSet<OrderState> OrderStates { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceOrder>().HasKey(e => e.Id);
        modelBuilder.Entity<OrderState>().HasKey(e => e.CorrelationId);
        modelBuilder.Entity<User>().HasKey(e => e.Id);
        
        // Configuração da Saga para o MassTransit
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
}
