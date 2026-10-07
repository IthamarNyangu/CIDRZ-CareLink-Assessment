# Decision briefing: mandatory reporting field

**To:** CIDRZ and Ministry leadership  
**Decision required:** How to introduce the new mandatory reporting field across 1,500 facilities

## Issue

The requested two-week national deadline is shorter than the six-week engineering estimate. This is not only a screen change: every facility database needs updating, synchronisation must support older offline clients, and the new external interface needs security review. Clinics remain open throughout.

## Main risk

Forcing the complete change into two weeks could block synchronisation, lose or duplicate reporting data, expose sensitive information and interrupt clinical work. A national launch date does not guarantee adoption because disconnected facilities may still run the previous version.

## Options

1. **Require the complete change nationally in two weeks.** This meets the announced date if every dependency succeeds, but has the highest risk of interruption, incompatible clients and unreliable reports. Engineering would reduce testing, pilot time or security assurance. I do not recommend it.

2. **Deliver safely in six weeks.** Engineering completes backward-compatible database and synchronisation changes, security review, pilot and staged rollout. This gives the greatest confidence in continuity and data quality, but delivers four weeks late.

3. **Use phased delivery, starting in two weeks.** Release the optional field behind a feature flag to a representative pilot and let national services accept old and new formats. Continue security review and monitor completion, sync failures and incidents. Expand in stages, then require the field after compatible versions reach the agreed facility coverage. Target full completion within six weeks.

## Recommendation

Choose option 3. It shows progress within two weeks without describing a partial rollout as national completion. Leadership should approve the compatibility window, name the policy owner for residual risk, and agree promotion criteria: successful migration and synchronisation, acceptable error rates, no unresolved high-risk security findings and tested rollback.

Reports will temporarily contain completed and missing values and must label this transition. If leadership still requires national enforcement in two weeks, it should formally accept the increased risk and record which assurance activities or facilities are deferred.
