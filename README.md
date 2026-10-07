# CareLink Follow Up

CareLink Follow Up is a thin, working vertical slice for identifying patients whose latest scheduled follow-up has been missed. The submission contains an ASP.NET Core API, a React and TypeScript screen, database migrations, deterministic sample-data generation, automated tests, and the written assessment sections.

## Current implementation status

The API vertical slice, migration, sample data, filtering, pagination, validation,
static role tokens, facility scoping, correlation IDs and API tests are implemented.
The React screen and written assessment sections remain in progress.

## Stack and rationale

- **ASP.NET Core on .NET 8:** This matches my strongest application-development experience and provides mature support for dependency injection, validation, logging, authorization and automated tests.
- **Entity Framework Core with SQLite:** SQLite is a relational database that runs locally without Docker or a licensed database server. It supports migrations, constraints, indexes and the required dataset size. The design can be moved to PostgreSQL or SQL Server in production.
- **React with TypeScript:** React is the preferred front-end option in the brief. TypeScript provides a typed API contract and helps prevent invalid UI states.
- **xUnit and Vitest:** These cover the server-side business rule and the front-end states judged most valuable.

## Repository structure

```text
src/
  CareLink.Api/          ASP.NET Core API
  carelink-web/          React and TypeScript client
tests/
  CareLink.Api.Tests/    API and rule tests
```

The required architecture, code-review, resilience, engineering-practice and executive-briefing documents will remain at the repository root so the panel can find them immediately.

## Follow up rule and assumptions

The brief deliberately leaves some edge cases open. This implementation uses the following interpretation:

1. The most recent visit is selected for each patient.
2. A patient is not in the follow-up worklist when that visit has no `next_appointment_date`.
3. Dates are compared as calendar dates, rather than local clock timestamps.
4. For the default threshold of seven days, an appointment must be strictly more than seven days late to be returned by the default query. An appointment exactly seven days late is a boundary case and is not yet overdue under this rule.
5. If the patient attended another visit after an earlier appointment date, that earlier missed appointment is no longer actionable. The new most-recent visit determines the patient's status.
6. Facility restrictions are enforced on the server. A clinician cannot obtain another facility's patients by changing a query parameter.

The brief asks the screen to show `due_soon`, `missed` and `overdue`, while the core endpoint description focuses on records more than the overdue threshold late. It also shows a response filtered by `status=overdue` that contains an item labelled `missed`. I resolve this as follows:

- `due_soon`: appointment date is today or within the next seven days;
- `missed`: appointment is one through `overdue_days` days late;
- `overdue`: appointment is more than `overdue_days` days late;
- no `status`: return the default overdue queue required by Section B1.

When a status filter is supplied, every returned item's `follow_up_status` must match that filter. This differs from the contradictory sample response and is documented so that the behaviour is predictable and testable.

## Running the API

```powershell
dotnet restore
dotnet test
dotnet run --project src/CareLink.Api -- --seed-demo
dotnet run --project src/CareLink.Api --launch-profile http
```

The first run applies the migration and creates deterministic demonstration data.
It is safe to run again because the seeder exits when data already exists. The second
run starts the API and Swagger UI at `http://localhost:5214/swagger`.

The protected endpoint is:

```text
GET /api/follow-up?facility_id=FAC-0101&status=overdue&overdue_days=7&page=1&page_size=50
```

Swagger's **Authorize** button accepts either demonstration bearer token:

- `manager-demo-token`: manager access to all facilities;
- `clinic-0101-demo-token`: clinic staff access to `FAC-0101` only.

These committed tokens are intentionally non-secret assessment fixtures. A production
deployment would use an identity provider, short-lived signed tokens, secret management,
auditing and a formal user-to-facility assignment process.

Clients may provide an `X-Correlation-ID` request header. The API returns it in the
response header and problem response so operational teams can match a reported failure
to its server log. If none is supplied, the API generates one.

To recreate the assessment-size performance database instead of the small demo:

```powershell
$env:ConnectionStrings__CareLink = 'Data Source=carelink-volume.db'
dotnet run --project src/CareLink.Api -- --seed-volume=100000
```

This creates 100,000 patients and 400,000 visits in a separate ignored SQLite
file. See [PERFORMANCE.md](PERFORMANCE.md) for the measured query and results.

### Small demo versus volume database

The two local database files serve different purposes and are not combined:

| Database | Contents | Purpose |
| --- | ---: | --- |
| `carelink.db` | 2 facilities, 8 patients, 8 visits | Readable examples for learning and UI demonstrations |
| `carelink-volume.db` | 150 facilities, 100,000 patients, 400,000 visits | Repeatable scale and performance verification |

The active database is selected through the `CareLink` connection string.
Swagger displays its filename near the top of the page. The follow-up endpoint
will never return all 100,000 patients: it first restricts data to one facility,
selects each patient's latest visit, filters to the requested status and then
returns only the requested page.

Run the React/TypeScript client in a second terminal after starting the API:

```powershell
cd src/carelink-web
npm install --legacy-peer-deps
npm run dev
```

Open `http://localhost:5173`. The Vite development server proxies `/api`
requests to the API at `http://localhost:5214`, so no browser CORS workaround
is required. Frontend verification commands are `npm run lint`, `npm test`
and `npm run build`.

## Scope discipline

The required GET endpoint, screen, tests, written sections, presentation and reproducible setup take priority. `POST /api/follow-up/{id}/contacted` is a stretch goal and will be added only after all required work is complete.
