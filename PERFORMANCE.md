# Performance Verification

## Purpose

The vertical slice was exercised against the assessment volume of 100,000
patients and 400,000 visits. The generated database is intentionally excluded
from Git; the command below recreates its counts and status distribution.

## Reproduction

From the repository root in PowerShell:

```powershell
$env:ConnectionStrings__CareLink = 'Data Source=carelink-volume.db'
dotnet run --project src/CareLink.Api -- --seed-volume=100000
dotnet run --project src/CareLink.Api --launch-profile http
```

The set-based volume generator creates 150 facilities, 100,000 patients and
exactly four visits per patient. It includes overdue, missed, due-soon and
no-next-appointment cases. Use the manager demonstration token in Swagger.

## Result recorded on 7 October 2026

Request:

```text
GET /api/follow-up?facility_id=FAC-0101&status=overdue&page=1&page_size=50
```

Local Windows development-machine result:

- dataset count: 100,000 patients and 400,000 visits;
- matching records for `FAC-0101`: 222;
- returned records: 50, confirming server-side pagination;
- first cold request: 381 ms;
- 10 warm requests: median 35 ms, maximum 98 ms.

These are development-machine observations, not a production service-level
guarantee. Production performance should be verified using representative
hardware, concurrency, network conditions and monitoring.

## Query-plan observation

SQLite's `EXPLAIN QUERY PLAN` showed that the query used:

- `IX_patient_facility_id` to restrict patients to the requested facility;
- `IX_visit_patient_id_visit_date_created_at` to find and compare each
  patient's visits;
- the same visit index as a covering index for the latest-visit subquery.

Only the requested page is materialised by the API. Status filtering, counting,
sorting and pagination remain database operations.
