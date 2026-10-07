using Microsoft.EntityFrameworkCore;

namespace CareLink.Api.Data;

public static class VolumeDataSeeder
{
    private const int FacilityCount = 150;
    private const int VisitsPerPatient = 4;

    public static async Task SeedAsync(
        CareLinkDbContext dbContext,
        int patientCount,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        if (patientCount < 1 || patientCount > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(patientCount),
                "Patient count must be between 1 and 1,000,000.");
        }

        if (await dbContext.Patients.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "Volume seeding requires an empty database. Use a separate SQLite file.");
        }

        var todayText = today.ToString("yyyy-MM-dd");
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($$"""
            WITH RECURSIVE numbers(n) AS (
                SELECT 1
                UNION ALL
                SELECT n + 1 FROM numbers WHERE n < {{FacilityCount}}
            )
            INSERT INTO facility (facility_id, name, district)
            SELECT
                printf('FAC-%04d', n),
                printf('Demonstration Facility %04d', n),
                printf('District %02d', ((n - 1) % 20) + 1)
            FROM numbers;
            """, cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($$"""
            WITH RECURSIVE numbers(n) AS (
                SELECT 1
                UNION ALL
                SELECT n + 1 FROM numbers WHERE n < {{patientCount}}
            )
            INSERT INTO patient (
                id,
                facility_id,
                patient_number,
                first_name,
                last_name,
                date_of_birth,
                sex,
                phone_number,
                created_at)
            SELECT
                lower(hex(randomblob(16))),
                printf('FAC-%04d', ((n - 1) % {{FacilityCount}}) + 1),
                printf(
                    '%04d-%06d',
                    ((n - 1) % {{FacilityCount}}) + 1,
                    CAST((n - 1) / {{FacilityCount}} AS INTEGER) + 1),
                printf('Patient%06d', n),
                printf('Demo%06d', n),
                date('1980-01-01', printf('+%d days', n % 9000)),
                CASE WHEN n % 2 = 0 THEN 'Female' ELSE 'Male' END,
                printf('+26097%07d', n % 10000000),
                datetime({{todayText}}, '-180 days')
            FROM numbers;
            """, cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($$"""
            WITH visit_sequence(sequence_number) AS (
                SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
            )
            INSERT INTO visit (
                id,
                patient_id,
                visit_date,
                next_appointment_date,
                visit_type,
                created_at)
            SELECT
                lower(hex(randomblob(16))),
                p.id,
                date(
                    {{todayText}},
                    printf('-%d days', 150 - (v.sequence_number * 30))),
                CASE
                    WHEN v.sequence_number < {{VisitsPerPatient}} THEN
                        date(
                            {{todayText}},
                            printf('-%d days', 120 - (v.sequence_number * 30)))
                    WHEN CAST(substr(p.patient_number, 6) AS INTEGER) % 6 = 0 THEN NULL
                    WHEN CAST(substr(p.patient_number, 6) AS INTEGER) % 6 = 1 THEN date({{todayText}}, '-15 days')
                    WHEN CAST(substr(p.patient_number, 6) AS INTEGER) % 6 = 2 THEN date({{todayText}}, '-8 days')
                    WHEN CAST(substr(p.patient_number, 6) AS INTEGER) % 6 = 3 THEN date({{todayText}}, '-7 days')
                    WHEN CAST(substr(p.patient_number, 6) AS INTEGER) % 6 = 4 THEN date({{todayText}}, '-1 day')
                    ELSE date({{todayText}}, '+3 days')
                END,
                CASE WHEN v.sequence_number = 4 THEN 'FollowUp' ELSE 'Routine' END,
                datetime(
                    {{todayText}},
                    printf('-%d days', 150 - (v.sequence_number * 30)),
                    '+12 hours')
            FROM patient AS p
            CROSS JOIN visit_sequence AS v;
            """, cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("ANALYZE;", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
