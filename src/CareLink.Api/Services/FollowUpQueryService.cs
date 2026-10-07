using CareLink.Api.Contracts;
using CareLink.Api.Data;
using CareLink.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Api.Services;

public sealed class FollowUpQueryService(
    CareLinkDbContext dbContext,
    IDateProvider dateProvider) : IFollowUpQueryService
{
    public Task<PagedResponse<FollowUpItem>> GetAsync(
        FollowUpQuery query,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(query, cancellationToken);
    }

    private async Task<PagedResponse<FollowUpItem>> ExecuteAsync(
        FollowUpQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query.FacilityId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.OverdueDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Page);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.PageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.PageSize, 200);

        var today = dateProvider.Today;
        var overdueCutoff = today.AddDays(-query.OverdueDays);
        var dueSoonLimit = today.AddDays(query.OverdueDays);

        var candidates = dbContext.Visits
            .AsNoTracking()
            .Where(visit => visit.Patient.FacilityId == query.FacilityId)
            .Where(visit => !dbContext.Visits.Any(other =>
                other.PatientId == visit.PatientId &&
                (other.VisitDate > visit.VisitDate ||
                 (other.VisitDate == visit.VisitDate &&
                  other.CreatedAtUtc > visit.CreatedAtUtc))))
            .Where(visit => visit.NextAppointmentDate != null);

        candidates = query.Status switch
        {
            FollowUpStatus.DueSoon => candidates.Where(visit =>
                visit.NextAppointmentDate >= today &&
                visit.NextAppointmentDate <= dueSoonLimit),
            FollowUpStatus.Missed => candidates.Where(visit =>
                visit.NextAppointmentDate < today &&
                visit.NextAppointmentDate >= overdueCutoff),
            FollowUpStatus.Overdue => candidates.Where(visit =>
                visit.NextAppointmentDate < overdueCutoff),
            null => candidates.Where(visit =>
                visit.NextAppointmentDate < overdueCutoff),
            _ => throw new ArgumentOutOfRangeException(nameof(query.Status))
        };

        candidates = query.Sort.ToLowerInvariant() switch
        {
            "days_overdue_asc" => candidates.OrderByDescending(visit => visit.NextAppointmentDate),
            "days_overdue_desc" => candidates.OrderBy(visit => visit.NextAppointmentDate),
            "patient_number_asc" => candidates.OrderBy(visit => visit.Patient.PatientNumber),
            _ => throw new ArgumentException("Unsupported sort value.", nameof(query.Sort))
        };

        var total = await candidates.CountAsync(cancellationToken);
        var rows = await candidates
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(visit => new
            {
                visit.Patient.Id,
                visit.Patient.PatientNumber,
                visit.Patient.FirstName,
                visit.Patient.LastName,
                visit.Patient.FacilityId,
                FacilityName = visit.Patient.Facility.Name,
                AppointmentDate = visit.NextAppointmentDate!.Value,
                visit.Patient.PhoneNumber
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row =>
        {
            var daysOverdue = today.DayNumber - row.AppointmentDate.DayNumber;
            var status = row.AppointmentDate >= today
                ? FollowUpStatus.DueSoon
                : row.AppointmentDate >= overdueCutoff
                    ? FollowUpStatus.Missed
                    : FollowUpStatus.Overdue;

            return new FollowUpItem(
                row.Id,
                row.PatientNumber,
                row.FirstName,
                row.LastName,
                row.FacilityId,
                row.FacilityName,
                row.AppointmentDate,
                daysOverdue,
                status,
                row.PhoneNumber,
                LastContactAttempt: null);
        }).ToArray();

        return new PagedResponse<FollowUpItem>(
            query.Page,
            query.PageSize,
            total,
            items);
    }
}
