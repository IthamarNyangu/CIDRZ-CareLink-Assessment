# Decision Log

This file records important decisions in plain language so that every implementation choice can be explained during the interview.

## 1 Use a modular monolith

**Decision:** Keep the assessment API in one deployable application with clear internal modules.

**Reason:** The delivery team is small and has nine months. A modular monolith reduces deployment and operational complexity while retaining clear boundaries.

**Rejected alternative:** Independent microservices. They would add network failure modes, distributed tracing, service deployment and data-consistency work before the team has demonstrated a need for that complexity.

## 2 Use SQLite locally

**Decision:** Use SQLite for the take-home implementation and document PostgreSQL or SQL Server as production options.

**Reason:** It is relational, repeatable from a clean machine and requires no Docker or licensed server. The query will still be tested against the required data volume.

**Rejected alternative:** A server database installed specifically for the assessment. That would make reviewer setup harder without proving additional judgement.

## 3 Treat status definitions as explicit assumptions

**Decision:** Define mutually exclusive status ranges and require filtered results to match the requested status.

**Reason:** The brief's prose and sample response conflict. Explicit, tested semantics are safer than hidden assumptions.

## 4 Keep the contacted endpoint as a stretch goal

**Decision:** Implement the required GET endpoint before the optional contacted workflow.

**Reason:** A correct, tested, documented vertical slice carries more value than two incomplete endpoints.

## Learning checkpoints

Before each commit, be able to answer:

- What requirement does this change satisfy?
- What assumption does it depend on?
- What failure or boundary does a test demonstrate?
- What would have to change in a national production deployment?

