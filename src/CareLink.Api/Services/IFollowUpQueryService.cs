using CareLink.Api.Contracts;

namespace CareLink.Api.Services;

public interface IFollowUpQueryService
{
    Task<PagedResponse<FollowUpItem>> GetAsync(
        FollowUpQuery query,
        CancellationToken cancellationToken = default);
}
