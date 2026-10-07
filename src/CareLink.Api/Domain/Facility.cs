namespace CareLink.Api.Domain;

public sealed class Facility
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required string District { get; set; }

    public ICollection<Patient> Patients { get; } = new List<Patient>();
}
