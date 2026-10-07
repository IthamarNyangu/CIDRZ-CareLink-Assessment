using CareLink.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Api.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(
        CareLinkDbContext dbContext,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Facilities.AnyAsync(cancellationToken))
        {
            return;
        }

        var facilities = new[]
        {
            new Facility
            {
                Id = "FAC-0101",
                Name = "Mwansa Urban Clinic",
                District = "Mwansa"
            },
            new Facility
            {
                Id = "FAC-0202",
                Name = "Chibombo Rural Health Centre",
                District = "Chibombo"
            }
        };
        dbContext.Facilities.AddRange(facilities);

        AddPatient(dbContext, "FAC-0101", "0101-000001", "Mary", "Banda", today.AddDays(-15));
        AddPatient(dbContext, "FAC-0101", "0101-000002", "John", "Phiri", today.AddDays(-8));
        AddPatient(dbContext, "FAC-0101", "0101-000003", "Ruth", "Mulenga", today.AddDays(-7));
        AddPatient(dbContext, "FAC-0101", "0101-000004", "Peter", "Zulu", today.AddDays(-1));
        AddPatient(dbContext, "FAC-0101", "0101-000005", "Agnes", "Tembo", today);
        AddPatient(dbContext, "FAC-0101", "0101-000006", "Brian", "Mbewe", today.AddDays(3));
        AddPatient(dbContext, "FAC-0101", "0101-000007", "Chanda", "Sakala", null);
        AddPatient(dbContext, "FAC-0202", "0202-000001", "Martha", "Lungu", today.AddDays(-12));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void AddPatient(
        CareLinkDbContext dbContext,
        string facilityId,
        string patientNumber,
        string firstName,
        string lastName,
        DateOnly? nextAppointmentDate)
    {
        var patient = new Patient
        {
            FacilityId = facilityId,
            PatientNumber = patientNumber,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = new DateOnly(1990, 1, 1),
            Sex = PatientSex.Female,
            PhoneNumber = "+260 97 000 0000"
        };
        patient.Visits.Add(new Visit
        {
            PatientId = patient.Id,
            VisitDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-45),
            NextAppointmentDate = nextAppointmentDate,
            VisitType = VisitType.Routine
        });
        dbContext.Patients.Add(patient);
    }
}
