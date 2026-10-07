namespace CareLink.Api.Infrastructure;

public interface IDateProvider
{
    DateOnly Today { get; }
}

public sealed class SystemDateProvider : IDateProvider
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
