export type FollowUpStatus = 'due_soon' | 'missed' | 'overdue'
export type FollowUpSort =
  | 'days_overdue_asc'
  | 'days_overdue_desc'
  | 'patient_number_asc'
export type AccessProfile = 'manager' | 'clinic'

export interface FollowUpItem {
  id: string
  patientNumber: string
  firstName: string
  lastName: string
  facilityId: string
  facilityName: string
  appointmentDate: string
  daysOverdue: number
  status: FollowUpStatus
  phoneNumber: string | null
  lastContactAttempt: string | null
}

export interface FollowUpResponse {
  page: number
  pageSize: number
  total: number
  items: FollowUpItem[]
}

export interface FollowUpQuery {
  facilityId: string
  status: FollowUpStatus
  overdueDays: number
  sort: FollowUpSort
  page: number
  pageSize: number
}

interface ProblemResponse {
  title?: string
  detail?: string
  correlationId?: string
  errors?: Record<string, string[]>
}

const tokens: Record<AccessProfile, string> = {
  manager: 'manager-demo-token',
  clinic: 'clinic-0101-demo-token',
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly correlationId?: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

export async function getFollowUps(
  query: FollowUpQuery,
  accessProfile: AccessProfile,
  signal?: AbortSignal,
): Promise<FollowUpResponse> {
  const parameters = new URLSearchParams({
    facility_id: query.facilityId,
    status: query.status,
    overdue_days: query.overdueDays.toString(),
    sort: query.sort,
    page: query.page.toString(),
    page_size: query.pageSize.toString(),
  })

  const response = await fetch(`/api/follow-up?${parameters}`, {
    headers: {
      Authorization: `Bearer ${tokens[accessProfile]}`,
      'X-Correlation-ID': crypto.randomUUID(),
    },
    signal,
  })

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemResponse
    const validationMessage = problem.errors
      ? Object.values(problem.errors).flat().join(' ')
      : undefined

    throw new ApiError(
      response.status,
      validationMessage ?? problem.detail ?? problem.title ?? 'The request failed.',
      problem.correlationId ?? response.headers.get('X-Correlation-ID') ?? undefined,
    )
  }

  return (await response.json()) as FollowUpResponse
}
