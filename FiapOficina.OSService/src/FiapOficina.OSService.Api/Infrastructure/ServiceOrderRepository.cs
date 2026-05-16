using FiapOficina.OSService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FiapOficina.OSService.Api.Infrastructure;

public interface IServiceOrderRepository
{
    Task<ServiceOrder?> GetByIdAsync(Guid id);
    Task AddAsync(ServiceOrder order);
    Task UpdateAsync(ServiceOrder order);
}

public class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly OSDbContext _context;

    public ServiceOrderRepository(OSDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceOrder?> GetByIdAsync(Guid id)
    {
        return await _context.ServiceOrders.FindAsync(id);
    }

    public async Task AddAsync(ServiceOrder order)
    {
        _context.ServiceOrders.Add(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ServiceOrder order)
    {
        _context.ServiceOrders.Update(order);
        await _context.SaveChangesAsync();
    }
}
