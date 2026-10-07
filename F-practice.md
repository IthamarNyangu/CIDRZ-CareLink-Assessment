# 90-day delivery and engineering-practice plan

## Position and first priorities

As a Software Engineer, I would model good practice, provide evidence and propose changes to the engineering lead rather than claim management authority. Clinical services continue, so improvement must be incremental.

My first two changes would be:

1. **Route every production change through version control, review and an auditable release path.** Direct fixes create unknown production state. Emergencies may use a fast path, but still need approval, rollback and an immediate reviewed commit.
2. **Create a small reliable CI gate.** Build, lint and high-value tests should protect new changes. An unreliable full suite should not become a gate until flaky tests are repaired.

## Days 1-30: stabilise and expose the work

### Change

- Agree a short working standard covering security, logging, style, definition of done and small pull requests.
- Use short-lived branches and protected `main`; require one reviewer and stable CI checks. Pair on authentication, migrations and clinical rules.
- Stop routine server edits and document the emergency process: approval, backup/rollback, minimal fix, monitoring and source-control reconciliation.
- Identify stable smoke tests for login, patient search, encounter saving and follow-up. Quarantine flaky tests visibly with an owner and expiry.
- Build one versioned artefact in CI. Triage the 300-item backlog with the business analyst into incidents, product outcomes and technical debt; remove duplicates and assign owners/priorities.
- Store current API documentation and runbooks beside the code. Review escaped defects weekly without blame.

### Leave alone for now

Do not rewrite the platform, chase a coverage percentage, automate production immediately or estimate all historical debt. First make the basic path trustworthy.

### Evidence

Normal production changes have linked commits, reviews and deployment records; stable CI stays green; the untriaged backlog falls; and baselines exist for change-failure rate, escaped defects, lead time, deployment duration, recovery time and flaky tests.

## Days 31-60: make releases repeatable

### Change

- Add tests around recent defects and critical workflows: rule tests, database/migration integration tests and a few end-to-end journeys. Repair flaky tests by root cause.
- Automate staging deployment, migration rehearsal, configuration checks, smoke tests and rollback instructions.
- Use backward-compatible expand-and-contract migrations so old and new facility clients can coexist.
- Release smaller changes on a regular cadence with notes, an owner, go/no-go checklist, support plan and representative pilot sites.
- Generate/check API documentation from code. Reserve capacity for debt ranked by clinical risk, recurrence, change friction and cost of delay.
- Pair senior and junior engineers on real changes and rotate reviewers, explaining reasons rather than only requesting corrections.

### Leave alone for now

Do not introduce microservices, continuous production deployment or an arbitrary organisation-wide coverage gate. Migration and rollback evidence must mature first.

### Evidence

Staging deployment and rollback rehearsal are repeatable; review/deployment lead time falls without more escaped defects; flaky and recurring defects decline; documentation ships with changes; and pilots find issues before national rollout.

## Days 61-90: control production delivery

### Change

- Create an approved production pipeline using signed/versioned artefacts, audit records, health checks and staged rollout.
- Use feature flags and a canary group covering reliable, intermittent and offline facilities. Pause on clinic-facing thresholds such as sync backlog or encounter-save errors.
- Add laboratory/reporting contract tests, older-client compatibility checks, restore tests and offline upgrade/recovery tests.
- Review a balanced dashboard monthly: change failures, escaped/recurring defects, lead and recovery time, deployment frequency, sync freshness and stakeholder outcomes.
- Refine the backlog jointly with programme teams using outcomes and acceptance examples. Record consequential design decisions and continue pairing and short learning sessions.

### Leave alone for now

Do not measure individual velocity, turn coverage into a target or promise all backlog/debt work. Larger architecture or team changes require evidence from the first 90 days.

### Evidence

Releases take less time and rarely need hotfixes; untracked server edits reach zero; failed changes recover within an agreed time; clinic error/sync measures stay within pilot thresholds; and defect recurrence, stale documentation and backlog age trend down.

## How I would behave

I would keep changes small, test every defect fix, update documentation, request review early and raise risk with evidence rather than blame. When requirements change, I would show the effect on scope and offer smaller safe slices. I would mentor through pairing while seeking senior guidance where I lack operational context.
