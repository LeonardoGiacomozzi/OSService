using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Threading.Tasks;

namespace FiapOficina.OSService.Tests;

public class ServiceOrderRepositoryTests
{
    private readonly OSDbContext _context;
    private readonly ServiceOrderRepository _repository;

    public ServiceOrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OSDbContext(options);
        _repository = new ServiceOrderRepository(_context);
    }

    [Fact]
    public async Task GetClientByIdentifierAsync_ShouldReturnNull_WhenClientDoesNotExist()
    {
        // Act
        var result = await _repository.GetClientByIdentifierAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetClientByIdentifierAsync_ShouldReturnClient_WhenClientExists()
    {
        // Arrange
        var client = new Client
        {
            Id = Guid.NewGuid(),
            Identifier = "12345678909",
            Name = "John Doe"
        };
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetClientByIdentifierAsync("12345678909");

        // Assert
        result.Should().NotBeNull();
        result!.Identifier.Should().Be("12345678909");
        result.Name.Should().Be("John Doe");
    }

    [Fact]
    public async Task AddClientAsync_ShouldSaveClientToDatabase()
    {
        // Arrange
        var client = new Client
        {
            Id = Guid.NewGuid(),
            Identifier = "99999999999",
            Name = "Jane Doe",
            Phone = "999",
            Email = "jane@doe.com",
            Address = "Road 2"
        };

        // Act
        await _repository.AddClientAsync(client);

        // Assert
        var savedClient = await _context.Clients.FirstOrDefaultAsync(c => c.Identifier == "99999999999");
        savedClient.Should().NotBeNull();
        savedClient!.Name.Should().Be("Jane Doe");
        savedClient.Phone.Should().Be("999");
        savedClient.Email.Should().Be("jane@doe.com");
        savedClient.Address.Should().Be("Road 2");
    }
}
