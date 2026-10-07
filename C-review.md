# Code review

## Review method

I ranked each issue by its likely effect in a clinic, not only by code style:

- **Critical:** can expose or corrupt health data, bypass access controls, or falsely report that clinical work was saved.
- **Major:** can make the system unreliable, unusably slow, or inaccessible during routine work.
- **Minor:** reduces maintainability or clarity but is unlikely to interrupt care by itself.

## C1: back-end service

| Rank | Problem | Practical risk in a clinic | Required direction |
| --- | --- | --- | --- |
| Critical | Database address, administrator username and password are embedded in source code. | Anyone who obtains the code or logs can gain broad database access; rotating the credential requires a release. | Store secrets outside source control, use a least-privileged application identity and rotate the exposed credential. |
| Critical | User-controlled facility, search and encounter values are concatenated into SQL. | SQL injection could expose records from other facilities, alter data or destroy records. Apostrophes in names/notes can also break legitimate requests. | Use parameterised commands or a correctly configured ORM for every value. |
| Critical | `SaveEncounter` performs two related writes without a transaction and silently catches every exception. | A visit might be inserted without updating the patient, or nothing may be saved while the clinician believes it succeeded. | Use one transaction, roll it back on failure, log a safe correlation ID and return/throw a defined error. |
| Critical | The caller supplies `facility` and `userRole`; neither is authorised against a trusted identity. | A user can request another facility's patient records or describe themselves as a different role. | Obtain identity and assignments from authenticated claims and enforce facility scope on the server. |
| Major | Connections, commands and readers are not disposed. | Under morning load the connection pool can be exhausted and clinical screens can stop responding. | Use `using`/`await using` and cancellation tokens. |
| Major | `SELECT *` retrieves unspecified columns. | More sensitive information than needed may be read, and schema changes can unexpectedly affect mapping and performance. | Select only the required columns using an explicit contract. |
| Major | Search has no validation, result limit or pagination. | An empty or broad search can load thousands of patients and consume memory on clinic computers. | Validate input and apply deterministic server-side pagination. |
| Critical | Logs contain the raw search and the first patient's national identifier. | Search terms and patient identifiers can leak through operational logs and backups. | Log only safe operational fields such as result count, facility scope, duration and correlation ID. |
| Major | `DueForFollowUp` loads patients and then visits one patient at a time. | This N+1 query pattern becomes very slow for large facility lists and increases database load. | Express the latest-visit rule as a single indexed, paginated database query. |
| Major | The follow-up rule adds a patient for every historic overdue visit and uses `DateTime.Now`. | Patients can appear more than once or appear despite a later visit; results vary with server timezone and cannot be tested reliably. | Select the latest visit, apply the no-later-visit rule, compare calendar dates through an injected clock, and return each patient once. |
| Minor | The class combines searching, saving, follow-up rules, SQL and logging. | Changes are harder to test and review, increasing regression risk. | Separate query, command and follow-up responsibilities behind small interfaces. |

### Three C1 issues I would block before merge

1. SQL injection and the embedded privileged credential.
2. Missing trusted facility authorisation.
3. Non-transactional encounter saving with swallowed failures.

The performance issues are serious, but the first three can immediately expose data, cross a facility boundary, or lose clinical work without warning.

## C2: front-end component

| Rank | Problem | Practical risk in a clinic | Required direction |
| --- | --- | --- | --- |
| Major | Props, state and patient values use `any`. | Invalid responses can reach rendering or update code unnoticed and fail at runtime. | Define typed props, patient records, filters and API response/error contracts. |
| Major | Fetching is inside the presentation component with no shared service or response validation. | Authentication, error handling and correlation behaviour become inconsistent across screens and are difficult to test. | Move calls into a typed API service. |
| Major | The initial request assumes every response is JSON success and has no catch/finally path. | A 401, 500 or network failure can leave "loading" displayed forever with no explanation or retry. | Check `response.ok`; represent loading, success, empty and error separately; provide retry. |
| Major | The polling effect has no dependency array. | A new interval is created after every render, causing repeated requests, poor performance and extra load on limited connectivity. | Add deliberate dependencies or use a tested polling/query abstraction; pause when offline or hidden. |
| Major | Requests are not cancelled and older responses can overwrite newer facility results. | Rapid facility changes can display the wrong facility's patients, creating a confidentiality and care risk. | Use `AbortController` or request identity checks. |
| Major | `patients.sort` mutates React state, and its effect also sets that same state. | It can trigger render loops or stale/inconsistent ordering. | Request server-side sorting for large data, or derive a copied array with `useMemo`. |
| Major | All patients are loaded, filtered and rendered in the browser. | A 10,000-record list can freeze older 4 GB clinic computers and wastes bandwidth. | Use server-side filtering/sorting and pagination or virtualisation. |
| Critical | `markContacted` mutates the patient object and shows success before checking the POST result. | Staff may believe outreach was recorded when the server rejected or never received it. | Await the response, handle failures, and update cached state immutably only after acknowledgement (or show a clearly pending queued action). |
| Critical | The POST has no visible authorisation, idempotency, request body, error handling or concurrency protection. | Repeated clicks or retries can create incorrect contact history, and read-only users may be offered a write. | Enforce server permission, send a typed body and idempotency key, and handle 401/403/409 responses. |
| Major | The filter input has only a placeholder, status is colour-only, and the clickable `div` is not keyboard operable. | Keyboard and assistive-technology users cannot reliably filter or mark a patient contacted. | Use a persistent `label`, text status, semantic `button`, focus styles and an announced result/error region. |
| Major | Array index is used as the row key. | Filtering/reordering can associate DOM state with the wrong patient. | Use the stable patient ID. |
| Minor | Inline status styles and mixed responsibilities make a shared clinical design difficult. | Behaviour and accessibility can diverge as screens are added. | Use shared semantic components and theme tokens. |

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
