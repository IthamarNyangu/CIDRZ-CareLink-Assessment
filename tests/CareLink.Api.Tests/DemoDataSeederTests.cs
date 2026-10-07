using CareLink.Api.Contracts;
using CareLink.Api.Data;
using CareLink.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Api.Tests;

public sealed class DemoDataSeederTests : IAsyncDisposable
{
    private static readonly DateOnly Today = new(2026, 10, 7);
    private readonly SqliteConnection _connection;
    private readonly CareLinkDbContext _dbContext;

    public DemoDataSeederTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CareLinkDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new CareLinkDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Seed_creates_balanced_status_examples_with_unique_phone_numbers()
    {
        await DemoDataSeeder.SeedAsync(_dbContext, Today);

        Assert.Equal(2, await _dbContext.Facilities.CountAsync());
        Assert.Equal(17, await _dbContext.Patients.CountAsync());
        Assert.Equal(17, await _dbContext.Visits.CountAsync());
        Assert.Equal(17, await _dbContext.Patients.Select(patient => patient.PhoneNumber).Distinct().CountAsync());

        var service = new FollowUpQueryService(_dbContext, new FixedDateProvider(Today));
        foreach (var status in new[]
                 {
                     FollowUpStatus.Overdue,
                     FollowUpStatus.Missed,
                     FollowUpStatus.DueSoon
                 })
        {
            var result = await service.GetAsync(new FollowUpQuery("FAC-0101", status));
            Assert.Equal(5, result.Total);
            Assert.Equal(5, result.Items.Count);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
