using CareLink.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Api.Tests;

public sealed class VolumeDataSeederTests
{
    [Fact]
    public async Task Seeder_creates_four_visits_for_every_patient()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CareLinkDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new CareLinkDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        await VolumeDataSeeder.SeedAsync(
            dbContext,
            patientCount: 60,
            today: new DateOnly(2026, 10, 7));

        Assert.Equal(60, await dbContext.Patients.CountAsync());
        Assert.Equal(240, await dbContext.Visits.CountAsync());
        Assert.Equal(
            4,
            await dbContext.Visits
                .GroupBy(visit => visit.PatientId)
                .Select(group => group.Count())
                .MinAsync());
        Assert.Equal(
            4,
            await dbContext.Visits
                .GroupBy(visit => visit.PatientId)
                .Select(group => group.Count())
                .MaxAsync());
    }
}
