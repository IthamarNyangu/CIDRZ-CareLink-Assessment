namespace CareLink.Api.Domain;

public sealed class Visit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PatientId { get; init; }
    public DateOnly VisitDate { get; set; }
    public DateOnly? NextAppointmentDate { get; set; }
    public VisitType VisitType { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Patient Patient { get; init; } = null!;
}

public enum VisitType
{
    Routine,
    FollowUp,
    Unscheduled
}
