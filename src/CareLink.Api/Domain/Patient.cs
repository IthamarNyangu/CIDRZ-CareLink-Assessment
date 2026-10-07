namespace CareLink.Api.Domain;

public sealed class Patient
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string FacilityId { get; init; }
    public required string PatientNumber { get; init; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public PatientSex Sex { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Facility Facility { get; init; } = null!;
    public ICollection<Visit> Visits { get; } = new List<Visit>();
}

public enum PatientSex
{
    Female,
    Male,
    Other
}
