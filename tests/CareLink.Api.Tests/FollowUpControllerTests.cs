using CareLink.Api.Contracts;
using CareLink.Api.Controllers;
using CareLink.Api.Security;
using CareLink.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CareLink.Api.Tests;

public sealed class FollowUpControllerTests
{
    [Fact]
    public async Task Get_maps_snake_case_parameters_to_the_query_service()
    {
        var service = new CapturingFollowUpQueryService();
        var controller = new FollowUpController(
            service,
            new AllowFacilityAccessService(),
            NullLogger<FollowUpController>.Instance);

        var response = await controller.GetAsync(
            facilityId: "FAC-0101",
            status: "due_soon",
            overdueDays: 10,
            sort: "patient_number_asc",
            page: 2,
            pageSize: 25);

        Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(
            new FollowUpQuery(
                "FAC-0101",
                FollowUpStatus.DueSoon,
                10,
                "patient_number_asc",
                2,
                25),
            service.CapturedQuery);
    }

    [Fact]
    public async Task Get_returns_validation_problem_for_invalid_parameters()
    {
        var service = new CapturingFollowUpQueryService();
        var controller = new FollowUpController(
            service,
            new AllowFacilityAccessService(),
            NullLogger<FollowUpController>.Instance);

        var response = await controller.GetAsync(
            facilityId: null,
            status: "unknown",
            overdueDays: 0,
            sort: "unknown",
            page: 0,
            pageSize: 201);

        var result = Assert.IsType<ObjectResult>(response.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Equal(6, problem.Errors.Count);
        Assert.Null(service.CapturedQuery);
    }

    private sealed class CapturingFollowUpQueryService : IFollowUpQueryService
    {
        public FollowUpQuery? CapturedQuery { get; private set; }

        public Task<PagedResponse<FollowUpItem>> GetAsync(
            FollowUpQuery query,
            CancellationToken cancellationToken = default)
        {
            CapturedQuery = query;
            return Task.FromResult(
                new PagedResponse<FollowUpItem>(
                    query.Page,
                    query.PageSize,
                    Total: 0,
                    Items: []));
        }
    }

    private sealed class AllowFacilityAccessService : IFacilityAccessService
    {
        public bool CanAccess(
            System.Security.Claims.ClaimsPrincipal user,
            string facilityId) => true;
    }
}
