using CareLink.Api.Contracts;
using CareLink.Api.Data;
using CareLink.Api.Domain;
using CareLink.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Api.Tests;

public sealed class FollowUpQueryServiceTests : IAsyncDisposable
{
    private static readonly DateOnly Today = new(2026, 10, 7);
    private readonly SqliteConnection _connection;
    private readonly CareLinkDbContext _dbContext;

    public FollowUpQueryServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CareLinkDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new CareLinkDbContext(options);
        _dbContext.Database.EnsureCreated();
        _dbContext.Facilities.Add(new Facility
        {
            Id = "FAC-0101",
            Name = "Mwansa Urban Clinic",
            District = "Mwansa"
        });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task Default_query_includes_patient_more_than_seven_days_late()
    {
        AddPatient("0101-000001", Today.AddDays(-8));
        await _dbContext.SaveChangesAsync();

        var result = await CreateService().GetAsync(new FollowUpQuery("FAC-0101"));

        var item = Assert.Single(result.Items);
        Assert.Equal(FollowUpStatus.Overdue, item.Status);
        Assert.Equal(8, item.DaysOverdue);
    }

    [Fact]
    public async Task Default_query_excludes_patient_exactly_seven_days_late()
    {
        AddPatient("0101-000002", Today.AddDays(-7));
        await _dbContext.SaveChangesAsync();

        var result = await CreateService().GetAsync(new FollowUpQuery("FAC-0101"));

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Missed_filter_includes_patient_exactly_seven_days_late()
    {
        AddPatient("0101-000003", Today.AddDays(-7));
        await _dbContext.SaveChangesAsync();

        var result = await CreateService().GetAsync(
            new FollowUpQuery("FAC-0101", FollowUpStatus.Missed));

        var item = Assert.Single(result.Items);
        Assert.Equal(FollowUpStatus.Missed, item.Status);
    }

    [Fact]
    public async Task Patient_with_no_next_appointment_is_not_returned()
    {
        AddPatient("0101-000004", nextAppointmentDate: null);
        await _dbContext.SaveChangesAsync();

        var result = await CreateService().GetAsync(new FollowUpQuery("FAC-0101"));

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Later_visit_replaces_the_old_missed_appointment()
    {
        var patient = AddPatient("0101-000005", Today.AddDays(-12));
        patient.Visits.Add(new Visit
        {
            PatientId = patient.Id,
            VisitDate = Today.AddDays(-2),
            NextAppointmentDate = Today.AddDays(28),
            VisitType = VisitType.FollowUp
        });
        await _dbContext.SaveChangesAsync();

        var result = await CreateService().GetAsync(new FollowUpQuery("FAC-0101"));

        Assert.Empty(result.Items);
    }

    private FollowUpQueryService CreateService() =>
        new(_dbContext, new FixedDateProvider(Today));

    private Patient AddPatient(string patientNumber, DateOnly? nextAppointmentDate)
    {
        var patient = new Patient
        {
            FacilityId = "FAC-0101",
            PatientNumber = patientNumber,
            FirstName = "Test",
            LastName = "Patient",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Sex = PatientSex.Female
        };
        patient.Visits.Add(new Visit
        {
            PatientId = patient.Id,
            VisitDate = Today.AddDays(-40),
            NextAppointmentDate = nextAppointmentDate,
            VisitType = VisitType.Routine
        });
        _dbContext.Patients.Add(patient);
        return patient;
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
