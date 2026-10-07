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
