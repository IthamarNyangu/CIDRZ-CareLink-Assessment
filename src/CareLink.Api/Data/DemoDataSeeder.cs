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

        // Five examples per visible status keep the default demonstration readable
        // while making filtering and pagination controls meaningful.
        AddPatient(dbContext, "FAC-0101", "0101-000001", "Mary", "Banda", "+260 97 100 0001", today.AddDays(-30));
        AddPatient(dbContext, "FAC-0101", "0101-000002", "John", "Phiri", "+260 97 100 0002", today.AddDays(-15));
        AddPatient(dbContext, "FAC-0101", "0101-000003", "Ruth", "Mulenga", "+260 97 100 0003", today.AddDays(-12));
        AddPatient(dbContext, "FAC-0101", "0101-000004", "Peter", "Zulu", "+260 97 100 0004", today.AddDays(-9));
        AddPatient(dbContext, "FAC-0101", "0101-000005", "Agnes", "Tembo", "+260 97 100 0005", today.AddDays(-8));

        AddPatient(dbContext, "FAC-0101", "0101-000006", "Brian", "Mbewe", "+260 96 200 0006", today.AddDays(-7));
        AddPatient(dbContext, "FAC-0101", "0101-000007", "Chanda", "Sakala", "+260 96 200 0007", today.AddDays(-6));
        AddPatient(dbContext, "FAC-0101", "0101-000008", "Grace", "Chileshe", "+260 96 200 0008", today.AddDays(-4));
        AddPatient(dbContext, "FAC-0101", "0101-000009", "Moses", "Nyirenda", "+260 96 200 0009", today.AddDays(-2));
        AddPatient(dbContext, "FAC-0101", "0101-000010", "Lillian", "Daka", "+260 96 200 0010", today.AddDays(-1));

        AddPatient(dbContext, "FAC-0101", "0101-000011", "Kelvin", "Mwila", "+260 95 300 0011", today);
        AddPatient(dbContext, "FAC-0101", "0101-000012", "Esther", "Hamaimbo", "+260 95 300 0012", today.AddDays(1));
        AddPatient(dbContext, "FAC-0101", "0101-000013", "Joseph", "Siame", "+260 95 300 0013", today.AddDays(3));
        AddPatient(dbContext, "FAC-0101", "0101-000014", "Naomi", "Mumba", "+260 95 300 0014", today.AddDays(5));
        AddPatient(dbContext, "FAC-0101", "0101-000015", "Daniel", "Musonda", "+260 95 300 0015", today.AddDays(7));

        AddPatient(dbContext, "FAC-0101", "0101-000016", "Chipo", "Nkhoma", "+260 76 400 0016", null);
        AddPatient(dbContext, "FAC-0202", "0202-000001", "Martha", "Lungu", "+260 76 400 0017", today.AddDays(-12));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void AddPatient(
        CareLinkDbContext dbContext,
        string facilityId,
        string patientNumber,
        string firstName,
        string lastName,
        string phoneNumber,
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
            PhoneNumber = phoneNumber
        };
        patient.Visits.Add(new Visit
        {
            PatientId = patient.Id,
            VisitDate = nextAppointmentDate?.AddDays(-30) ?? new DateOnly(2020, 1, 1),
            NextAppointmentDate = nextAppointmentDate,
            VisitType = VisitType.Routine
        });
        dbContext.Patients.Add(patient);
    }
}
