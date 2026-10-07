import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import {
  ApiError,
  getFollowUps,
} from './api/followUpApi'
import type {
  AccessProfile,
  FollowUpItem,
  FollowUpQuery,
  FollowUpResponse,
  FollowUpSort,
  FollowUpStatus,
} from './api/followUpApi'

const initialQuery: FollowUpQuery = {
  facilityId: 'FAC-0101',
  status: 'overdue',
  overdueDays: 7,
  sort: 'days_overdue_desc',
  page: 1,
  pageSize: 50,
}

type RequestState =
  | { kind: 'loading' }
  | { kind: 'success'; data: FollowUpResponse }
  | { kind: 'error'; error: ApiError }

function App() {
  const [accessProfile, setAccessProfile] = useState<AccessProfile>('manager')
  const [draftQuery, setDraftQuery] = useState(initialQuery)
  const [query, setQuery] = useState(initialQuery)
  const [requestState, setRequestState] = useState<RequestState>({ kind: 'loading' })

  useEffect(() => {
    const controller = new AbortController()
    setRequestState({ kind: 'loading' })

    getFollowUps(query, accessProfile, controller.signal)
      .then((data) => setRequestState({ kind: 'success', data }))
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === 'AbortError') return
        setRequestState({
          kind: 'error',
          error:
            error instanceof ApiError
              ? error
              : new ApiError(0, 'The API could not be reached. Confirm that the backend is running.'),
        })
      })

    return () => controller.abort()
  }, [query, accessProfile])

  const data = requestState.kind === 'success' ? requestState.data : undefined
  const pageCount = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1

  const rangeLabel = useMemo(() => {
    if (!data || data.total === 0) return '0 records'
    const start = (data.page - 1) * data.pageSize + 1
    const end = Math.min(data.page * data.pageSize, data.total)
    return `${start}–${end} of ${data.total}`
  }, [data])

  function submitFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setQuery({ ...draftQuery, facilityId: draftQuery.facilityId.trim(), page: 1 })
  }

  function changePage(page: number) {
    setDraftQuery((current) => ({ ...current, page }))
    setQuery((current) => ({ ...current, page }))
  }

  function retry() {
    setQuery((current) => ({ ...current }))
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="#main-content" aria-label="CareLink home">
          <span className="brand-mark" aria-hidden="true">
            <EcgIcon />
          </span>
          <span>
            <strong>CareLink</strong>
            <small>Continuity of care</small>
          </span>
        </a>
        <div className="topbar-actions">
          <div className="environment-chip">
            <span className="environment-dot" aria-hidden="true" />
            Demonstration environment
          </div>
          <label className="nav-access-control">
            <span>Access profile</span>
            <select
              value={accessProfile}
              onChange={(event) => setAccessProfile(event.target.value as AccessProfile)}
            >
              <option value="manager">CareLink manager · all facilities</option>
              <option value="clinic">Clinic staff · FAC-0101 only</option>
            </select>
          </label>
        </div>
      </header>

      <main id="main-content" className="page-content">
        <section className="page-heading" aria-labelledby="page-title">
          <div>
            <h1 id="page-title">Patient follow-up</h1>
            <p className="heading-copy">
              Review missed appointments and coordinate patient outreach.
            </p>
          </div>
        </section>

        <section className="summary-grid" aria-label="Queue summary">
          <SummaryCard
            label="Patients in this queue"
            value={requestState.kind === 'success' ? requestState.data.total.toLocaleString() : '—'}
            detail="After filters are applied"
            icon={<PeopleIcon />}
          />
          <SummaryCard
            label="Current facility"
            value={query.facilityId || 'Not selected'}
            detail={data?.items[0]?.facilityName ?? 'Facility-scoped access'}
            icon={<ClinicIcon />}
          />
          <SummaryCard
            label="Overdue threshold"
            value={`${query.overdueDays} days`}
            detail="Strictly beyond this window"
            icon={<CalendarIcon />}
          />
        </section>

        <section className="workspace" aria-label="Follow-up worklist">
          <form className="filter-panel" onSubmit={submitFilters}>
            <div className="filter-heading">
              <div>
                <p className="eyebrow">Queue controls</p>
                <h2>Filter patients</h2>
              </div>
              <FilterIcon />
            </div>

            <label>
              <span>Facility ID</span>
              <input
                required
                value={draftQuery.facilityId}
                onChange={(event) =>
                  setDraftQuery((current) => ({ ...current, facilityId: event.target.value }))
                }
                placeholder="FAC-0101"
              />
            </label>

            <label>
              <span>Follow-up status</span>
              <select
                value={draftQuery.status}
                onChange={(event) =>
                  setDraftQuery((current) => ({
                    ...current,
                    status: event.target.value as FollowUpStatus,
                  }))
                }
              >
                <option value="overdue">Overdue</option>
                <option value="missed">Missed</option>
                <option value="due_soon">Due soon</option>
              </select>
            </label>

            <div className="filter-row">
              <label>
                <span>Threshold in days</span>
                <input
                  type="number"
                  min="1"
                  max="365"
                  required
                  value={draftQuery.overdueDays}
                  onChange={(event) =>
                    setDraftQuery((current) => ({
                      ...current,
                      overdueDays: Number(event.target.value),
                    }))
                  }
                />
              </label>

              <label>
                <span>Rows per page</span>
                <select
                  value={draftQuery.pageSize}
                  onChange={(event) =>
                    setDraftQuery((current) => ({
                      ...current,
                      pageSize: Number(event.target.value),
                    }))
                  }
                >
                  <option value="10">10</option>
                  <option value="25">25</option>
                  <option value="50">50</option>
                  <option value="100">100</option>
                </select>
              </label>
            </div>

            <label>
              <span>Sort order</span>
              <select
                value={draftQuery.sort}
                onChange={(event) =>
                  setDraftQuery((current) => ({
                    ...current,
                    sort: event.target.value as FollowUpSort,
                  }))
                }
              >
                <option value="days_overdue_desc">Most overdue first</option>
                <option value="days_overdue_asc">Least overdue first</option>
                <option value="patient_number_asc">Patient number</option>
              </select>
            </label>

            <button className="primary-button" type="submit">
              <SearchIcon />
              Apply filters
            </button>
          </form>

          <div className="results-panel" aria-live="polite">
            <div className="results-toolbar">
              <div>
                <p className="eyebrow">Action queue</p>
                <h2>{statusHeading(query.status)}</h2>
              </div>
              <span className="result-count">{rangeLabel}</span>
            </div>

            {requestState.kind === 'loading' && <LoadingState />}
            {requestState.kind === 'error' && (
              <ErrorState error={requestState.error} onRetry={retry} />
            )}
            {requestState.kind === 'success' && requestState.data.items.length === 0 && (
              <EmptyState />
            )}
            {requestState.kind === 'success' && requestState.data.items.length > 0 && (
              <>
                <FollowUpTable items={requestState.data.items} />
                <nav className="pagination" aria-label="Follow-up results pages">
                  <button
                    type="button"
                    disabled={requestState.data.page <= 1}
                    onClick={() => changePage(requestState.data.page - 1)}
                  >
                    <ChevronLeftIcon /> Previous
                  </button>
                  <span>
                    Page <strong>{requestState.data.page}</strong> of {pageCount}
                  </span>
                  <button
                    type="button"
                    disabled={requestState.data.page >= pageCount}
                    onClick={() => changePage(requestState.data.page + 1)}
                  >
                    Next <ChevronRightIcon />
                  </button>
                </nav>
              </>
            )}
          </div>
        </section>
      </main>
    </div>
  )
}

function SummaryCard({
  label,
  value,
  detail,
  icon,
}: {
  label: string
  value: string
  detail: string
  icon: React.ReactNode
}) {
  return (
    <article className="summary-card">
      <span className="summary-icon" aria-hidden="true">
        {icon}
      </span>
      <div>
        <p>{label}</p>
        <strong>{value}</strong>
        <small>{detail}</small>
      </div>
    </article>
  )
}

function FollowUpTable({ items }: { items: FollowUpItem[] }) {
  return (
    <div className="table-scroll">
      <table>
        <caption className="sr-only">Patients requiring follow-up</caption>
        <thead>
          <tr>
            <th scope="col">Patient</th>
            <th scope="col">Appointment</th>
            <th scope="col">Priority</th>
            <th scope="col">Contact</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td data-label="Patient">
                <div className="patient-cell">
                  <span className="avatar" aria-hidden="true">
                    {item.firstName[0]}
                    {item.lastName[0]}
                  </span>
                  <span>
                    <strong>{item.firstName} {item.lastName}</strong>
                    <small>{item.patientNumber}</small>
                  </span>
                </div>
              </td>
              <td data-label="Appointment">
                <strong>{formatDate(item.appointmentDate)}</strong>
                <small>{appointmentTiming(item.daysOverdue)}</small>
              </td>
              <td data-label="Priority">
                <span className={`status-badge status-${item.status}`}>
                  <span aria-hidden="true" />
                  {statusLabel(item.status)}
                </span>
              </td>
              <td data-label="Contact">
                <strong>{item.phoneNumber ?? 'Not recorded'}</strong>
                <small>{item.lastContactAttempt ? 'Previous attempt recorded' : 'No contact attempt'}</small>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function LoadingState() {
  return (
    <div className="loading-state" role="status">
      <span className="spinner" aria-hidden="true" />
      <strong>Loading the follow-up queue…</strong>
      <p>Applying facility access and appointment rules.</p>
    </div>
  )
}

function EmptyState() {
  return (
    <div className="message-state">
      <span className="message-icon success" aria-hidden="true"><CheckIcon /></span>
      <h3>No patients match these filters</h3>
      <p>Try another status or facility. This queue currently needs no action.</p>
    </div>
  )
}

function ErrorState({ error, onRetry }: { error: ApiError; onRetry: () => void }) {
  const content = errorContent(error)
  return (
    <div className="message-state error-state" role="alert">
      <span className="message-icon danger" aria-hidden="true"><WarningIcon /></span>
      <h3>{content.title}</h3>
      <p>{content.message}</p>
      {error.correlationId && <small>Reference: {error.correlationId}</small>}
      <button type="button" className="secondary-button" onClick={onRetry}>Try again</button>
    </div>
  )
}

function errorContent(error: ApiError) {
  if (error.status === 401) {
    return { title: 'Authentication required', message: 'Choose a valid access profile and try again.' }
  }
  if (error.status === 403) {
    return {
      title: 'Facility access denied',
      message: 'This profile is not authorised to view patients from the selected facility.',
    }
  }
  if (error.status === 400) {
    return { title: 'Check the filter values', message: error.message }
  }
  return { title: 'The queue could not be loaded', message: error.message }
}

function statusHeading(status: FollowUpStatus) {
  return status === 'due_soon'
    ? 'Appointments due soon'
    : status === 'missed'
      ? 'Recently missed appointments'
      : 'Overdue follow-ups'
}

function statusLabel(status: FollowUpStatus) {
  return status === 'due_soon' ? 'Due soon' : status[0].toUpperCase() + status.slice(1)
}

function appointmentTiming(daysOverdue: number) {
  if (daysOverdue > 0) return `${daysOverdue} ${daysOverdue === 1 ? 'day' : 'days'} late`
  if (daysOverdue === 0) return 'Due today'
  const daysUntil = Math.abs(daysOverdue)
  return `Due in ${daysUntil} ${daysUntil === 1 ? 'day' : 'days'}`
}

function formatDate(date: string) {
  return new Intl.DateTimeFormat('en-ZM', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  }).format(new Date(`${date}T00:00:00`))
}

const iconProps = { width: 20, height: 20, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', strokeWidth: 1.8, strokeLinecap: 'round' as const, strokeLinejoin: 'round' as const }
function EcgIcon() { return <svg {...iconProps}><path d="M2 12h5l2.5-6 5 12 2.5-6h5" /></svg> }
function PeopleIcon() { return <svg {...iconProps}><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" /></svg> }
function ClinicIcon() { return <svg {...iconProps}><path d="M3 21h18M5 21V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v16M9 21v-4h6v4M9 7h6M12 4v6" /></svg> }
function CalendarIcon() { return <svg {...iconProps}><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M16 3v4M8 3v4M3 11h18M8 15h.01M12 15h.01M16 15h.01" /></svg> }
function FilterIcon() { return <svg {...iconProps}><path d="M4 5h16M7 12h10M10 19h4" /></svg> }
function SearchIcon() { return <svg {...iconProps}><circle cx="11" cy="11" r="7" /><path d="m20 20-4-4" /></svg> }
function ChevronLeftIcon() { return <svg {...iconProps}><path d="m15 18-6-6 6-6" /></svg> }
function ChevronRightIcon() { return <svg {...iconProps}><path d="m9 18 6-6-6-6" /></svg> }
function CheckIcon() { return <svg {...iconProps}><path d="m5 12 4 4L19 6" /></svg> }
function WarningIcon() { return <svg {...iconProps}><path d="M10.3 2.9 1.8 17a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 2.9a2 2 0 0 0-3.4 0Z" /><path d="M12 9v4M12 17h.01" /></svg> }

export default App
