# Resilience, offline operation and data integrity

## D1: duplicate submissions

### Making replay safe

Every client write receives a UUID operation ID before it is first queued. The client sends it as an idempotency key on every attempt. The API stores, under a unique `(facility_id, operation_id)` constraint, a hash of the request and the resulting status/resource ID. The business write and idempotency record commit in one database transaction.

On replay, the server behaves as follows:

- same key and same request hash: return the original outcome without repeating the write;
- same key but different content: return `409 Conflict`, because reusing a key for another operation is a client defect;
- new key: validate authorisation, references, dates and concurrency, then process normally.

Creating a patient also uses the operation ID. In addition, the server checks the facility's unique patient number and uses matching signals to flag possible identity duplicates. An idempotency key prevents a repeated operation; it does not by itself detect two separately created records for the same person.

### Client responsibilities

The client must create and persist the operation ID before showing the action as locally saved. It must reuse that ID, body and authenticated user context on every retry, retain queued work across restarts, and not generate a new key after a timeout. Dependencies are recorded explicitly: for example, a visit referencing a newly created offline patient waits until the server has acknowledged or mapped that patient.

The interface distinguishes **saved locally**, **synchronising**, **synchronised**, **needs review** and **failed**. It must never turn a timeout into an unqualified success or ask the clinician to re-enter the action.

### Retention and expiry

I would retain idempotency outcomes for at least the maximum supported offline period plus the maximum retry and support window - initially 90 days, subject to measured outage and policy data. Completed keys can then be archived or compacted. The client must not automatically replay an expired operation: the API returns a defined response and the item enters supervised reconciliation, where staff can verify whether the clinical event already exists.

For records such as visits, a permanent globally unique clinical-event ID provides a second uniqueness boundary even after the short-term request record expires. Retention is monitored so a cleanup job cannot silently remove keys that still have queued clients.

### Repairing duplicates already present

I would first stop further duplication, take a recoverable backup and run a read-only detection report. Exact duplicate visits can be proposed using patient, facility, event time, type and source-operation evidence. Possible duplicate patients require stronger matching on facility number and carefully normalised demographic fields; automated similarity is only a candidate generator.

Clinical records are not simply deleted. An authorised data steward reviews each candidate with the facility. Confirmed patient duplicates are linked under a surviving identity through a transactional merge that repoints dependent records, preserves both original identifiers, records who approved the decision and writes an audit event. If provenance must be retained, the duplicate identity is marked inactive/merged rather than removed. Incorrectly duplicated visits are voided with a reason and audit trail. Ambiguous cases remain separate and flagged for review. Reports and downstream partners receive the corrected identity mapping so the same duplication is not reintroduced.

## D2: working through an outage

### Local data and retention

The facility client caches only its authorised operational subset in an encrypted local database: patient identity needed for care, recent/active clinical records, follow-up worklist, reference data, expiring user access information, sync checkpoints and pending actions. It does not cache national datasets, unrestricted records from other facilities, bulk reporting extracts, reusable passwords, access tokens longer than necessary, or secrets that can sign new credentials.

The cache has policy-based limits. Pending unsynchronised clinical actions remain until acknowledged or reconciled; downloaded data uses a clinically agreed rolling window and can be refreshed or removed after it is no longer operationally required. The screen shows the last successful sync time so users can judge freshness.

### Synchronisation after reconnection

The client first verifies the server and refreshes authentication. It uploads small durable batches in causal order per patient: a locally created patient before that patient's visit, for example. Independent patients can synchronise concurrently within conservative limits. Each operation is acknowledged individually and advances a local checkpoint only after durable server acceptance.

Transient failures such as timeouts and `5xx` responses retry with exponential back-off, jitter and a maximum rate. `401` pauses for authentication; `403` and validation failures do not loop; `409` creates a conflict task. A partial batch response removes only acknowledged operations. Resuming uses the existing keys and checkpoint, not the beginning of the queue. Downloads use a server sequence/cursor, are applied transactionally and are safe to repeat.

### Concurrent changes and user experience

Each editable record carries the server version on which the offline change was based. If a clinician edits version 4 on Monday and the national record becomes version 5 on Tuesday, Wednesday's upload does not silently overwrite version 5. The server returns a conflict containing safe field/version information.

Non-overlapping changes may be merged automatically according to an approved rule. A meaningful clinical conflict appears in a reconciliation screen showing the local change, current server value, authors and times. Both versions remain preserved. An authorised clinician or designated data steward decides the outcome according to clinical governance; software developers do not decide which clinical fact is true. Until resolved, the user sees **needs review**, not **synchronised**.

### Protecting data on a shared clinic computer

- Encrypt the local database and queue using a device-bound key protected by the operating system or secure hardware.
- Use named accounts, short inactivity locks, least privilege and time-limited offline grants; do not rely on a shared clinic login.
- Register devices, audit access, minimise background previews/notifications and prevent sensitive values from entering application logs.
- Keep application and database files outside casually browsable folders, restrict operating-system permissions, use full-disk encryption and provide a revocation/wipe process when the device reconnects.
- Expire cached data according to policy and securely remove data no longer required, while never deleting unacknowledged clinical work.

I would refuse to store national extracts, records from unrelated facilities, plaintext credentials, private signing keys, unnecessary attachments or complete patient histories without an offline care need. A lost device should expose the smallest possible encrypted dataset, not a portable copy of the national EHR.
