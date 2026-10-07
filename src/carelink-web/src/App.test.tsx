import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'

const successResponse = {
  page: 1,
  pageSize: 50,
  total: 2,
  items: [
    {
      id: 'patient-1',
      patientNumber: '0101-000001',
      firstName: 'Mary',
      lastName: 'Banda',
      facilityId: 'FAC-0101',
      facilityName: 'Mwansa Urban Clinic',
      appointmentDate: '2026-09-22',
      daysOverdue: 15,
      status: 'overdue',
      phoneNumber: '+260 97 000 0000',
      lastContactAttempt: null,
    },
  ],
}

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    headers: new Headers({ 'X-Correlation-ID': 'test-correlation' }),
    json: () => Promise.resolve(body),
  } as Response)
}

describe('CareLink follow-up screen', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('shows the loading state and then renders returned patients', async () => {
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse(successResponse)))

    render(<App />)

    expect(screen.getByText('Loading the follow-up queue…')).toBeInTheDocument()
    expect(await screen.findByText('Mary Banda')).toBeInTheDocument()
    expect(screen.getByText('15 days late')).toBeInTheDocument()
    expect(screen.getByText('1–2 of 2')).toBeInTheDocument()
    expect(screen.getByLabelText('Threshold days')).toBeInTheDocument()
  })

  it('explains when an authenticated user lacks facility access', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() => jsonResponse({ title: 'Forbidden', correlationId: 'access-test-403' }, 403)),
    )

    render(<App />)

    expect(await screen.findByText('Facility access denied')).toBeInTheDocument()
    expect(screen.getByText('Reference: access-test-403')).toBeInTheDocument()
  })

  it('sends changed filters to the API service', async () => {
    const fetchMock = vi.fn((_input: RequestInfo | URL) =>
      jsonResponse({ ...successResponse, total: 0, items: [] }),
    )
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()
    render(<App />)
    await screen.findByText('No patients match these filters')

    await user.selectOptions(screen.getByLabelText('Follow-up status'), 'missed')
    await user.click(screen.getByRole('button', { name: 'Apply filters' }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))
    const requestUrl = String(fetchMock.mock.calls[1][0])
    expect(requestUrl).toContain('status=missed')
    expect(requestUrl).toContain('facility_id=FAC-0101')
  })

  it('provides a logical keyboard path from navigation into the worklist', async () => {
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse(successResponse)))
    const user = userEvent.setup()
    render(<App />)
    await screen.findByText('Mary Banda')

    await user.tab()
    expect(screen.getByRole('link', { name: 'CareLink home' })).toHaveFocus()
    await user.tab()
    expect(screen.getByLabelText('Access profile')).toHaveFocus()
    await user.tab()
    expect(screen.getByLabelText('Facility ID')).toHaveFocus()
  })
})
