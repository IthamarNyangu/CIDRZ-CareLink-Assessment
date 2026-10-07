# Code review

## Review method

I ranked each issue by its likely effect in a clinic, not only by code style:

- **Critical:** can expose or corrupt health data, bypass access controls, or falsely report that clinical work was saved.
- **Major:** can make the system unreliable, unusably slow, or inaccessible during routine work.
- **Minor:** reduces maintainability or clarity but is unlikely to interrupt care by itself.

## C1: back-end service

| Rank | Problem | Practical risk in a clinic |
| --- | --- | --- |
| Critical | Database address and administrator credential are embedded in source. | Code disclosure gives broad database access, and rotation requires a release. |
| Critical | Facility, search and encounter values are concatenated into SQL. | Injection could expose/alter other facilities' data; apostrophes also break valid input. |
| Critical | `SaveEncounter` has no transaction and catches every exception. | Only one write may succeed, or nothing saves while the clinician believes it did. |
| Critical | The caller supplies `facility` and `userRole` without trusted authorisation. | A caller can request another facility or claim another role. |
| Major | Connections, commands and readers are not disposed. | Morning load can exhaust connections and stop clinical screens. |
| Major | `SELECT *` retrieves unspecified columns. | It reads unnecessary sensitive data and makes schema changes unpredictable. |
| Major | Search has no validation, limit or pagination. | A broad search can load thousands of patients on an old computer. |
| Critical | Logs contain search text and a national identifier. | Sensitive data can leak through logs and backups. |
| Major | `DueForFollowUp` queries visits once per patient. | The N+1 pattern becomes very slow on large lists. |
| Major | It returns every historic overdue visit and uses `DateTime.Now`. | Patients duplicate or remain despite later attendance; timezone-dependent results are hard to test. |
| Minor | One class handles search, saving, follow-up, SQL and logging. | Coupling makes changes harder to test and review. |

### Three C1 issues I would block before merge

1. SQL injection and the embedded privileged credential.
2. Missing trusted facility authorisation.
3. Non-transactional encounter saving with swallowed failures.

The performance issues are serious, but the first three can immediately expose data, cross a facility boundary, or lose clinical work without warning.

## C2: front-end component

| Rank | Problem | Practical risk in a clinic |
| --- | --- | --- |
| Major | Props, state and patients use `any`. | Invalid responses can fail at runtime unnoticed. |
| Major | Fetching sits inside the component without a typed service. | Authentication, errors and correlation handling become inconsistent and hard to test. |
| Major | The request assumes JSON success and has no failure/finally path. | Network or server failure can leave "loading" forever without retry. |
| Major | The polling effect has no dependency array. | Every render creates another interval, overloading weak connections and the server. |
| Major | Requests are not cancelled. | An older response may show patients from the wrong facility. |
| Major | `sort` mutates state and the effect resets that state. | It can cause loops and inconsistent ordering. |
| Major | All patients are loaded and rendered. | 10,000 rows can freeze older 4 GB computers and waste bandwidth. |
| Critical | `markContacted` mutates state and shows success before acknowledgement. | Staff may believe outreach was recorded when the server rejected it. |
| Critical | The POST lacks visible authorisation, idempotency and error handling. | Retries can create bad contact history and read-only users may see a write. |
| Major | Placeholder-only input, colour status and clickable `div`. | Keyboard and assistive-technology users cannot reliably act. |
| Major | Array index is the row key. | Reordering can attach interface state to the wrong patient. |
| Minor | Inline styling and mixed responsibilities. | Screens become inconsistent and harder to maintain. |

### Three C2 issues I would block before merge

1. False-success and direct mutation in `markContacted`.
2. The uncontrolled polling interval that is recreated on every render.
3. Missing error/retry handling and request race protection.

I would also require the keyboard/accessibility defects before releasing to clinics, even if they were repaired in a follow-up commit before the release branch rather than the first review revision.

## Refactor of C1

I chose the back-end extract because its worst defects cross facility boundaries, expose a database credential and can silently lose clinical data. The focused refactor below addresses three areas: trusted facility authorisation plus parameterised search, transactional encounter saving, and safe error handling. A further change would move `DueForFollowUp` into a dedicated indexed query like the Section B implementation.

```csharp
public sealed class PatientService(
    IDbConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    ILogger<PatientService> logger)
{
    public async Task<IReadOnlyList<PatientSummary>> SearchAsync(
        string query,
        string requestedFacilityId,
        CancellationToken cancellationToken)
    {
        if (!currentUser.CanReadFacility(requestedFacilityId))
            throw new ForbiddenException("Facility access is not permitted.");

        var normalisedQuery = (query ?? string.Empty).Trim();
        if (normalisedQuery.Length > 100)
            throw new ValidationException("Search text cannot exceed 100 characters.");

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, patient_number, first_name, last_name
            FROM patient
            WHERE facility_id = @facility_id
              AND (@query = ''
                   OR first_name LIKE @pattern
                   OR last_name LIKE @pattern)
            ORDER BY last_name, first_name, id
            LIMIT 100;
            """;
        command.AddParameter("@facility_id", requestedFacilityId);
        command.AddParameter("@query", normalisedQuery);
        command.AddParameter("@pattern", $"%{EscapeLike(normalisedQuery)}%");

        var results = await command.ReadPatientsAsync(cancellationToken);
        logger.LogInformation(
            "Patient search completed for facility {FacilityId}; ResultCount={ResultCount}",
            requestedFacilityId,
            results.Count);
        return results;
    }

    public async Task SaveEncounterAsync(
        EncounterCommand encounter,
        CancellationToken cancellationToken)
    {
        if (!currentUser.CanWriteFacility(encounter.FacilityId))
            throw new ForbiddenException("Facility write access is not permitted.");

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO encounter (patient_id, visit_date, notes)
                VALUES (@patient_id, @visit_date, @notes);
                """,
                new
                {
                    patient_id = encounter.PatientId,
                    visit_date = encounter.VisitDate,
                    notes = encounter.Notes
                },
                transaction,
                cancellationToken);

            var updated = await connection.ExecuteAsync(
                """
                UPDATE patient
                SET last_visit_date = @visit_date
                WHERE id = @patient_id AND facility_id = @facility_id;
                """,
                new
                {
                    patient_id = encounter.PatientId,
                    facility_id = encounter.FacilityId,
                    visit_date = encounter.VisitDate
                },
                transaction,
                cancellationToken);

            if (updated != 1)
                throw new NotFoundException("Patient was not found in the permitted facility.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            logger.LogError(exception, "Encounter save failed");
            throw;
        }
    }
}
```

`IDbConnectionFactory` obtains its connection information from protected configuration and returns a least-privileged connection. `ICurrentUser` is populated from verified authentication claims rather than request parameters. The sample deliberately logs no patient ID, notes, name or national identifier; the request correlation ID would be added by the logging scope/middleware.

In production I would also validate dates and note length, use an idempotency key for offline replay, add an optimistic concurrency value, and integration-test rollback and facility isolation. Those concerns are important, but keeping the refactor focused makes the three merge-blocking changes easy to review.
