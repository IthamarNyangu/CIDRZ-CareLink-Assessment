# 90-day delivery and engineering-practice plan

## Position and priorities

As a Software Engineer, I would not unilaterally reorganise the team or impose policy. I would model the practices in my own work, make the current problems measurable, propose small changes to the Senior Software Engineer and help teammates adopt them. Clinical services must continue, so the plan reduces risk incrementally rather than freezing delivery.

The first two changes I would push for are:

1. **All production changes go through version control, review and an auditable release path.** Direct server fixes create unknown production state, bypass review and make recovery difficult. An emergency path can be fast, but must still produce a reviewed commit and deployment record.
2. **Create a small reliable CI quality gate around changed code.** Stable build, lint and high-value tests give reviewers fast evidence and prevent more defects while the broader test suite is repaired. Requiring an unreliable full suite immediately would teach people to ignore failures.

## Days 1-30: stabilise and make work visible

### Change now

- Agree a short working agreement: formatting, naming, secure handling, error/logging rules, definition of done and small pull requests.
- Use short-lived feature/fix branches with protected `main`; require one reviewer and passing build/lint/stable tests. Pair on sensitive migrations, authentication and clinical rules.
- Stop routine direct production editing. Document a time-boxed emergency process: incident approval, backup/rollback, smallest change, monitoring, then a pull request immediately after service restoration.
- Identify a small smoke/regression suite for login, patient lookup, encounter saving and follow-up. Quarantine flaky tests visibly with owners and expiry dates rather than silently rerunning them.
- Add CI that produces one versioned build artefact and dependency/security findings. The same artefact should progress toward each environment.
- Triage the 300-item backlog with the business analyst and programme representatives: remove duplicates, identify incidents/regulatory deadlines, add an owner and outcome, and separate product work from technical debt.
- Put current API behaviour and runbooks next to the code. Add a documentation check to the definition of done.
- Start a weekly defect review without blame: source, escaped test, affected release and prevention action.

### Deliberately leave alone

I would not pursue a large platform rewrite, chase a coverage percentage, change every branching convention, automate production deployment immediately or estimate all technical debt. Those changes would consume attention before the release process is trustworthy.

### Evidence it worked

- 100% of normal production changes have a linked commit, review and deployment record.
- CI produces a reproducible artefact and stays green for agreed stable checks.
- Backlog items have category, owner and priority; the oldest untriaged count falls sharply.
- Baselines exist for change-failure rate, escaped defects, lead time, deployment duration, rollback time, flaky tests and API-document age.

## Days 31-60: make releases repeatable

### Change now

- Expand tests around recent defect clusters and critical clinical workflows, using a test pyramid: many rule/unit tests, database integration tests for migrations and constraints, and a few end-to-end journeys.
- Repair or replace flaky tests by root cause; do not reward raw test counts. Assign changed-code tests during refinement and review.
- Automate deployment to a production-like staging environment, including migration rehearsal, configuration validation, smoke tests and rollback instructions.
- Adopt backward-compatible expand-and-contract migrations so older facility clients and new servers can coexist during rollout.
- Introduce a regular, smaller release cadence with release notes, named release owner, go/no-go checklist, support contact and a representative pilot group.
- Generate/OpenAPI-check API documentation from code, add examples and nominate an owner for each integration contract.
- Reserve an explicit portion of capacity for measured technical debt and operational defects. Rank debt by patient/service risk, frequency, change friction and cost of delay.
- Pair a senior/junior engineer on one real change each cycle and rotate review partners; use review comments to teach the reason, not only demand a correction.

### Deliberately leave alone

I would not require full continuous deployment to clinics, split the system into microservices or block releases on an arbitrary organisation-wide coverage target. Connectivity, migration and rollback evidence must mature first.

### Evidence it worked

- Staging deployment is automated and repeatable; migration and rollback rehearsals are recorded.
- Median review and deployment lead time falls without an increase in escaped defects.
- Flaky-test rate and defects recurring from the same cause decline.
- API changes and release notes are published with the release, not months later.
- Pilot releases detect issues before national rollout and rollback time is measured.

## Days 61-90: controlled delivery and continuous improvement

### Change now

- Automate a controlled production pipeline with approvals, signed/versioned artefacts, audit trail, health checks and staged rollout. Production remains protected from manual file changes.
- Use feature flags and a canary group across reliable, intermittent and offline facilities. Pause or roll back based on clinic-facing thresholds such as login failures, sync backlog and encounter-save errors.
- Add contract tests for laboratory/reporting integrations and supported older clients. Test restore procedures and offline upgrade/recovery, not only backups.
- Hold a monthly product/engineering review using a small balanced dashboard: change-failure rate, escaped clinical defects, lead time, deployment frequency, recovery time, sync freshness, flaky tests and stakeholder outcomes.
- Convert the prioritised backlog into outcome-based items with acceptance examples. Programme teams join refinement early; engineers expose uncertainty and slice work before commitment.
- Create short design records for consequential decisions and a rotating technical-debt review. Continue pairing, short learning sessions and supportive review calibration.

### Deliberately leave alone

I would not optimise teams against individual velocity, use coverage as a performance target, or promise that all 300 backlog items and historical debt will be completed. Those incentives encourage gaming and hide risk. Larger architecture or team-structure changes require evidence from the first 90 days.

### Evidence it worked

- Releases no longer take three days and rarely require an immediate hotfix; failed changes recover within an agreed time.
- Direct, untracked production edits are zero, including emergencies being reconciled afterward.
- Clinic-facing error and sync-freshness measures remain within thresholds during staged rollout.
- Defect recurrence, stale documentation and backlog age trend down for several cycles.
- Programme and engineering representatives can describe the same priorities, acceptance conditions and release risks.

## How I would behave in this team

I would keep pull requests small, add tests around every defect I fix, update documentation with behaviour changes, ask for review early and raise production risk with evidence rather than blame. When requirements change, I would make the effect on scope and delivery visible, offer smaller safe slices, and confirm decisions with the business analyst. I would help junior teammates through pairing and constructive reviews, while asking senior engineers for guidance on areas where I lack operational context.
