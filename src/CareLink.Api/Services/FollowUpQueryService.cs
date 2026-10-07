using CareLink.Api.Contracts;
using CareLink.Api.Data;
using CareLink.Api.Infrastructure;

namespace CareLink.Api.Services;

public sealed class FollowUpQueryService(
    CareLinkDbContext dbContext,
    IDateProvider dateProvider) : IFollowUpQueryService
{
    public Task<PagedResponse<FollowUpItem>> GetAsync(
        FollowUpQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = dbContext;
        _ = dateProvider;
        throw new NotImplementedException("The query will be implemented after the boundary tests are in place.");
    }
}
