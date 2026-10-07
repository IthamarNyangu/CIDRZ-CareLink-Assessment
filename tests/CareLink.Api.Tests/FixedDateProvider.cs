using CareLink.Api.Infrastructure;

namespace CareLink.Api.Tests;

internal sealed class FixedDateProvider(DateOnly today) : IDateProvider
{
    public DateOnly Today { get; } = today;
}
