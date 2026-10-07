namespace CareLink.Api.Contracts;

public enum FollowUpStatus
{
    DueSoon,
    Missed,
    Overdue
}

public sealed record FollowUpQuery(
    string FacilityId,
    FollowUpStatus? Status = null,
    int OverdueDays = 7,
    string Sort = "days_overdue_desc",
    int Page = 1,
    int PageSize = 50);

public sealed record FollowUpItem(
    Guid Id,
    string PatientNumber,
    string FirstName,
    string LastName,
    string FacilityId,
    string FacilityName,
    DateOnly AppointmentDate,
    int DaysOverdue,
    FollowUpStatus Status,
    string? PhoneNumber,
    DateTimeOffset? LastContactAttempt);

public sealed record PagedResponse<T>(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<T> Items);
