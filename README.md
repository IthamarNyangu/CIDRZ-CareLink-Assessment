# CareLink Follow-Up

CareLink Follow-Up is a working vertical slice for identifying patients who need follow-up after a missed appointment. It contains an ASP.NET Core API, React/TypeScript screen, relational schema and migration, deterministic seeders, access controls, automated tests and the assessment's written sections.

## What is implemented

- `GET /api/follow-up` with facility and status filtering, configurable overdue threshold, days-overdue sorting and server-side pagination.
- Latest-visit business rule evaluated in SQLite rather than application memory.
- EF Core migration from an empty database, database constraints and indexes.
- Small readable demonstration seed plus a reproducible 100,000-patient/400,000-visit scale seed.
- Static demonstration authentication, role/facility authorisation, consistent problem responses and correlation IDs.
- React screen with typed API service, loading, empty, error/retry and success states, filters, status text, responsive table/cards and pagination.
- Eleven backend tests and four frontend tests covering the business boundary, authorisation, validation and high-value UI states.

## Deliberately stubbed or left out

The offline facility client, synchronisation engine, laboratory/reporting integrations and production identity provider are architecture designs, not part of this thin vertical slice. Authentication uses two non-secret assessment tokens. `POST /api/follow-up/{id}/contacted` is not implemented; it was treated as a stretch goal after the required endpoint, screen and written work. Production hardening would also add rate limiting, audit storage, encrypted facility storage, broader browser/assistive-technology testing and deployment automation.

## Stack and rationale

- **ASP.NET Core on .NET 8:** strong typed contracts, dependency injection, validation, logging, authorisation and test support.
- **Entity Framework Core with SQLite:** a local relational database requiring no Docker or licensed server, while still supporting migrations, transactions, constraints and indexes. The production design would use an approved in-country PostgreSQL or SQL Server deployment.
- **React 18 with TypeScript and Vite:** the preferred frontend in the brief, with typed API boundaries and a small build/runtime footprint.
- **xUnit and Vitest/Testing Library:** rule and API tests on the server plus user-visible state tests in the client.

## Repository structure

```text
src/
  CareLink.Api/          ASP.NET Core API, migration and seeders
  carelink-web/          React and TypeScript client
tests/
  CareLink.Api.Tests/    API, rule, access and scale-seeder tests
A-architecture.md        Proposed national architecture and trade-offs
C-review.md              Ranked review of both supplied extracts and refactor
D-resilience.md          Duplicate replay, offline sync and data integrity
F-practice.md            90-day engineering-practice plan
G-briefing.md            Executive briefing (339 words)
INTERVIEW-QA.md          Study questions and model answers
PERFORMANCE.md           Reproducible scale-test method and observations
```

## Prerequisites

- .NET 8 SDK
- Node.js 20 or later with npm
- Git

SQLite is supplied through the .NET dependency; Docker, SSMS and a separate database server are not required.

## Run from a clean machine

Clone the repository and restore/build the backend:

```powershell
git clone <repository-url>
cd CIDRZ-CareLink-Assessment
dotnet restore
dotnet build --no-restore
```

Create the database from the committed migration and add the small demonstration data:

```powershell
dotnet run --project src/CareLink.Api -- --seed-demo
```

The seed command exits after creating `src/CareLink.Api/carelink.db`. It is idempotent: rerunning it does not duplicate the demonstration records.

Start the API:

```powershell
dotnet run --project src/CareLink.Api --launch-profile http
```

Open `http://localhost:5214/swagger`. Select **Authorize** and enter one of:

- `manager-demo-token`: reads all demonstration facilities;
- `clinic-0101-demo-token`: reads only `FAC-0101`.

In a second terminal, install and run the client:

```powershell
cd src/carelink-web
npm install --legacy-peer-deps
npm run dev
```

Open `http://localhost:5173`. Vite proxies `/api` to `http://localhost:5214`, so the browser and Swagger use the same API and small database.

## Run the tests

From the repository root:

```powershell
dotnet test
cd src/carelink-web
npm run lint
npm test
npm run build
```

I prioritised backend tests for the exact 7-day boundary, latest-visit rule, no-next-appointment rule, pagination/validation and facility isolation because errors there can omit or disclose patients. Frontend tests cover loading-to-success, empty, error/retry and access-profile behaviour because a blank or stale clinical queue could otherwise be misinterpreted.

## Small demo and assessment-scale data

The databases are intentionally separate and generated locally:

| Database | Generated contents | Purpose |
| --- | ---: | --- |
| `carelink.db` | 2 facilities, 8 patients, 8 visits | Default readable UI, Swagger and interview demonstration |
| `carelink-volume.db` | 150 facilities, 100,000 patients, 400,000 visits | Scale and query-plan verification |

Database files are ignored by Git. The migration and seeders are the reproducible submission artefacts.

To create and run the volume database in PowerShell:

```powershell
$env:ConnectionStrings__CareLink = 'Data Source=carelink-volume.db'
dotnet run --project src/CareLink.Api -- --seed-volume=100000
dotnet run --project src/CareLink.Api --launch-profile http
```

Swagger displays the active database filename. Clear the terminal-only override before returning to the demo database:

```powershell
Remove-Item Env:ConnectionStrings__CareLink
```

The scale run returned 50 of 222 matching `FAC-0101` records from 100,000 patients and 400,000 visits. Its first cold request took 381 ms; ten warm requests had a 35 ms median and 98 ms maximum on the development laptop. Server pagination reduces the browser's work from a possible 10,000 rendered records to at most 50 rows per request. These are local observations, not production guarantees; see [PERFORMANCE.md](PERFORMANCE.md).

## Follow-up rule and assumptions

1. The most recent visit is selected for each patient.
2. A patient is excluded when that visit has no `next_appointment_date`.
3. Dates are compared as calendar dates rather than local clock timestamps.
4. With a threshold of seven days, an appointment becomes `overdue` only when it is **more than** seven days late. Exactly seven days remains `missed`.
5. A later attended visit supersedes an earlier missed appointment. The new most-recent visit determines current follow-up status.
6. Facility access is enforced by the API, not only hidden in the interface.

The brief's sample request filters `status=overdue` but includes an item labelled `missed`. I chose consistent filter semantics:

- `due_soon`: due today through the next seven days;
- `missed`: one through `overdue_days` days late;
- `overdue`: strictly more than `overdue_days` days late;
- no status: the default overdue queue required by B1.

Every returned item's status therefore matches an explicitly supplied status filter.

## API behaviour and security

```text
GET /api/follow-up?facility_id=FAC-0101&status=overdue&overdue_days=7&sort=days_overdue_desc&page=1&page_size=50
```

Clients may supply `X-Correlation-ID`; otherwise the API generates one. It is returned in response headers and problem details so an error can be matched to safe structured logs. Logs do not include names, phone numbers, patient numbers or clinical content.

The committed tokens are fixtures, not secrets. Production would use the Ministry-approved identity provider, short-lived signed tokens, protected secret storage, audited role/geographic assignments and an offline-access policy.

## Frontend accessibility decisions

1. Filters have persistent programmatic labels; buttons and navigation use native semantic elements and visible keyboard focus.
2. Status always has a text label and is never communicated by colour alone. Error messages use an announced alert region and provide a retry path.
3. Locally bundled Inter, WCAG-AA colour combinations, strong input boundaries, responsive reflow and server pagination support readability on older laptops and at narrow widths.

I did not complete a full manual screen-reader/browser matrix or an exact interactive 200% zoom session within the assessment time. I inspected keyboard order, responsive layouts and high-DPI rendering, and would add NVDA plus supported-browser/200%-zoom acceptance checks before clinical release.

## Known limitations and next steps

Given another week I would:

1. implement the authorised, idempotent contacted endpoint and its UI state;
2. add integration tests against a production-target database engine and concurrent load tests;
3. test clean installation in a separate machine/container and add CI;
4. conduct NVDA, browser-compatibility and 200%-zoom testing with representative users;
5. prototype the encrypted offline queue and conflict workflow described in Section D.

The required GET endpoint, screen, migrations, seeders, tests and written analysis were prioritised over these extensions.
