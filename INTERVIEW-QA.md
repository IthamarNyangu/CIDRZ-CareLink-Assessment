# CareLink interview questions and model answers

Use these answers to understand the decisions, not as a script to memorise word for word. In the interview, start with the direct answer, explain the reason, and connect it to an effect on clinic staff or patient data.

## Section A: architecture and trade-offs

### 1. Why did you choose a modular monolith instead of microservices?

I chose a modular monolith because the team has nine engineers and nine months to deliver the first release. Microservices would introduce additional deployments, network calls, distributed transactions and monitoring requirements before we had evidence that we needed independent scaling. A modular monolith lets us deploy one application while keeping follow-up, identity, synchronisation and integrations separated by clear interfaces. If a module later needs independent scaling or deployment, those boundaries provide a path to extract it.

**Short version:** It gives this relatively small team simpler deployment and data consistency now, without giving up clean boundaries for future growth.

### 2. If a facility uploads the same visit twice after reconnecting, how does the server avoid creating a duplicate?

The client creates a unique operation ID before saving the visit locally and sends that same ID on every retry. The server stores the operation ID with the accepted result under a uniqueness constraint. On the first request, it records the visit and response in one transaction. If the same operation is replayed, the server recognises the ID and returns the original outcome instead of inserting another visit. This is an idempotent write: repeating the same operation has the same effect as performing it once.

The ID must be created before the first request and persisted in the offline queue. Generating a new ID for every retry would defeat the protection.

**Short version:** Every queued write has a stable idempotency key; retries reuse it, and the server processes each key only once.

### 3. Why should laboratory integration be asynchronous rather than part of the clinician's save request?

The clinician's ability to save a visit should not depend on the laboratory system being online or responding quickly. The clinical transaction is saved first, and a durable integration message is processed separately. Temporary laboratory failures can then be retried without making the user wait or enter the visit again. The integration also gains a visible queue, acknowledgements and reconciliation, so missing or rejected messages do not fail silently.

The trade-off is that the result is eventually consistent rather than immediately confirmed. The interface must show whether a laboratory message is pending, sent or failed.

**Short version:** A partner outage should delay message exchange, not stop clinical care or make the clinician lose work.

### 4. What is the difference between the facility database and the national database?

The facility database is a limited operational cache that supports local care during outages. It holds only the facility's authorised patients, recent records, follow-up information and pending operations. It is encrypted and may contain data that has not synchronised yet.

The national database is the authoritative longitudinal store. It combines accepted data from facilities, cross-facility patient identity, access assignments, audit history and reporting information. Facility changes become part of the national record only after synchronisation, validation and conflict handling.

**Short version:** The facility database keeps one clinic working offline; the national database is the authoritative country-level record after synchronisation.

### 5. Why does the system still authorise an offline action when it reaches the server?

An offline grant only proves what the user and device were allowed to do when they last connected. Their account, role or facility assignment may have changed during the outage. The server therefore checks the action against current access rules during synchronisation. An unauthorised action is retained for review and clearly rejected; it is not silently applied or discarded.

### 6. Why not copy the entire national database to every facility?

A full copy would expose unrelated patients, require more storage and synchronisation bandwidth, and leave large amounts of sensitive data on shared or stolen computers. Each facility should receive the smallest operational dataset it needs. Cross-facility access should be requested through authorised national services when connectivity permits.

### 7. What is your least certain architecture decision?

I am least certain about how much and how long clinical data should be cached at a facility. Too little data prevents useful work during a two-day outage; too much increases privacy, storage and stale-data risk. I would decide the retention window using measured outage durations, device capacity, clinical workflows and security policy, then verify it through a rural-facility pilot.

### 8. Why is a relational database appropriate for CareLink?

Patients, visits, facilities and appointments have clear relationships and integrity rules. A relational database provides foreign keys, unique constraints, transactions, indexed queries and controlled migrations. Those features help prevent orphaned visits, duplicate facility patient numbers and partially saved clinical operations. It also supports predictable operational and reporting queries.

## Section C: code review

### 1. What three backend issues would you block before merge?

I would block the embedded privileged database credential and SQL injection, the absence of trusted facility authorisation, and the non-transactional encounter save that swallows all exceptions. Together they can expose or corrupt data, allow cross-facility access, and tell a clinician nothing when a clinical record was not saved.

### 2. Why are parameterised queries important?

Parameters keep executable SQL separate from user-supplied values. A name, note or search string is treated as data rather than part of the command, preventing SQL injection and avoiding failures caused by characters such as apostrophes. Parameters do not replace authorisation or validation, but they are essential database protection.

### 3. Why must saving the encounter and updating the patient use one transaction?

The two writes represent one logical clinical operation. Without a transaction, the encounter insert might succeed while the patient's last-visit update fails, leaving contradictory records. A transaction commits both changes or rolls both back, so the database cannot retain a half-completed operation.

### 4. Why is an empty `catch` block dangerous?

It converts a real failure into apparent success. The clinician may move on believing the encounter was saved, support staff receive no useful signal, and data inconsistencies remain hidden. The service should roll back, log a safe error with its correlation ID and return a defined failure that the interface can explain and retry safely.

### 5. What is wrong with loading patients and then querying visits for every patient?

It creates an N+1 query pattern: one query loads the patients and then another query runs for each patient. With 10,000 patients that could mean 10,001 database calls. The latest-visit and overdue rules should be expressed as an indexed database query, followed by server-side sorting and pagination.

### 6. Why should logs not contain patient identifiers, search text or clinical notes?

Operational logs are copied, retained and accessed differently from the clinical database. Including patient data expands the number of places where sensitive information can leak. Logs should contain enough operational context to diagnose the event - such as correlation ID, safe facility scope, result count, duration and error category - without including clinical content.

### 7. What three frontend issues would you block before merge?

I would block the contacted action that shows success without checking the server, the polling effect that creates a new interval after every render, and the missing error/race handling for requests. They can create false clinical records, overload unreliable connections and display data from the wrong facility.

### 8. Why is mutating React state directly a problem?

React relies on state being replaced immutably so it can recognise changes and render predictably. Sorting the existing array or changing a patient object directly can produce stale views and unexpected effects. A component should create a new array or object, or refresh the confirmed result from the API.

### 9. Why must loading, empty and error states be separate?

They mean different things to the user. Loading means the answer is not available yet, empty means the request succeeded but no records matched, and error means the request failed and may need a retry or corrective action. Showing the same blank table for all three can make a service outage look like there are no patients requiring follow-up.

### 10. Why is a clickable `div` not an appropriate action control?

A `div` has no button semantics and does not automatically support keyboard activation, focus or disabled behaviour. A real `button` works with keyboards and assistive technology and communicates its purpose correctly. Styling should not replace native semantics.

## Section D: resilience, offline operation and data integrity

### 1. What is idempotency?

Idempotency means repeating the same operation produces the same result as performing it once. The client assigns a stable operation ID before queuing a write, and the server stores the outcome under that ID. If a timeout causes the client to replay it, the server returns the original outcome instead of inserting another visit.

### 2. Why must the client generate the idempotency key before sending the first request?

The key identifies one logical action across every attempt. If the client creates a different key after a timeout, the server cannot know that the requests represent the same visit and may accept both. The key and request body must be durably stored together before the interface says the work is saved locally.

### 3. Why is an idempotency key not enough to prevent every duplicate patient?

It prevents one operation from being processed twice. A user can still create the same person through two separate operations with two valid keys, or two devices may create that person independently while offline. Patient duplication also requires facility-number constraints, matching rules and human-reviewed identity reconciliation.

### 4. Why keep idempotency records after a request succeeds?

An offline client may retry days or weeks later because it never received the original acknowledgement. Keeping the result allows the server to recognise that late replay. Retention should cover the maximum supported outage, retry and support window; after expiry, an uncertain replay goes to reconciliation rather than being inserted automatically.

### 5. How would you handle duplicate clinical records that already exist?

First prevent new duplicates and take a recoverable backup. Generate candidates without changing data, then have an authorised data steward and facility review them. Confirmed patient duplicates are linked or merged transactionally under a surviving identity with original identifiers and audit history preserved. Incorrect visits are voided with a reason rather than silently deleted. Ambiguous records remain separate and flagged.

### 6. What does the client cache for offline work?

Only the minimum authorised facility data required for care: relevant patient identity, recent or active clinical records, the follow-up list, reference data, sync checkpoints and queued actions. Pending actions remain until acknowledged. The client should not hold national extracts, unrelated facilities' records, plaintext credentials or unnecessary complete histories.

### 7. How are queued actions synchronised after connectivity returns?

The client refreshes authentication, then uploads small batches in dependency order. Each operation has an idempotency key and receives its own acknowledgement. Timeouts and server failures retry with exponential back-off and jitter; authentication pauses for login; validation failures stop retrying; conflicts enter review. Only acknowledged items leave the queue, so partial failure does not replay completed work incorrectly.

### 8. What happens when an offline record and the national record were both changed?

The offline write includes the server version it was based on. If that version is stale, the server returns a conflict instead of overwriting newer data. Safe non-overlapping fields may merge under an agreed rule; clinical conflicts show both versions, authors and times to an authorised clinician or data steward. The system preserves both versions and displays `needs review` until resolved.

### 9. Why not use "last write wins" for clinical conflicts?

The latest timestamp does not prove that a clinical fact is correct, especially when device clocks can differ and one user may have better information. Last-write-wins can silently discard valid care data. Important conflicts need explicit rules or accountable human review with both versions preserved.

### 10. How do you protect sensitive data on a shared offline computer?

Minimise the cached dataset, encrypt the database with a device-bound key, use named accounts and automatic locks, enforce operating-system permissions and full-disk encryption, use expiring offline access, and keep patient data out of logs and notifications. Never remove unsynchronised work merely to satisfy a cache-retention limit.
