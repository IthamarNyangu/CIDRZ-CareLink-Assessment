using CareLink.Api.Contracts;
using CareLink.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.Api.Controllers;

[ApiController]
[Route("api/follow-up")]
public sealed class FollowUpController(
    IFollowUpQueryService followUpQueryService,
    ILogger<FollowUpController> logger) : ControllerBase
{
    private static readonly HashSet<string> SupportedSorts =
    [
        "days_overdue_asc",
        "days_overdue_desc",
        "patient_number_asc"
    ];

    [HttpGet]
    [ProducesResponseType<PagedResponse<FollowUpItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<FollowUpItem>>> GetAsync(
        [FromQuery(Name = "facility_id")] string? facilityId,
        [FromQuery] string? status,
        [FromQuery(Name = "overdue_days")] int overdueDays = 7,
        [FromQuery] string sort = "days_overdue_desc",
        [FromQuery] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var errors = Validate(
            facilityId,
            status,
            overdueDays,
            sort,
            page,
            pageSize,
            out var parsedStatus);

        if (errors.Count > 0)
        {
            foreach (var (key, messages) in errors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(key, message);
                }
            }

            return ValidationProblem(ModelState);
        }

        logger.LogInformation(
            "Retrieving follow-up queue for facility {FacilityId}, status {Status}, page {Page}",
            facilityId,
            parsedStatus?.ToString() ?? "overdue (default)",
            page);

        var result = await followUpQueryService.GetAsync(
            new FollowUpQuery(
                facilityId!,
                parsedStatus,
                overdueDays,
                sort,
                page,
                pageSize),
            cancellationToken);

        return Ok(result);
    }

    private static Dictionary<string, string[]> Validate(
        string? facilityId,
        string? status,
        int overdueDays,
        string sort,
        int page,
        int pageSize,
        out FollowUpStatus? parsedStatus)
    {
        var errors = new Dictionary<string, string[]>();
        parsedStatus = null;

        if (string.IsNullOrWhiteSpace(facilityId))
        {
            errors["facility_id"] = ["facility_id is required."];
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            !TryParseStatus(status, out parsedStatus))
        {
            errors["status"] = ["status must be due_soon, missed, or overdue."];
        }

        if (overdueDays < 1 || overdueDays > 365)
        {
            errors["overdue_days"] = ["overdue_days must be between 1 and 365."];
        }

        if (!SupportedSorts.Contains(sort))
        {
            errors["sort"] =
            ["sort must be days_overdue_asc, days_overdue_desc, or patient_number_asc."];
        }

        if (page < 1)
        {
            errors["page"] = ["page must be at least 1."];
        }

        if (pageSize < 1 || pageSize > 200)
        {
            errors["page_size"] = ["page_size must be between 1 and 200."];
        }

        return errors;
    }

    private static bool TryParseStatus(string value, out FollowUpStatus? status)
    {
        status = value.Trim().ToLowerInvariant() switch
        {
            "due_soon" => FollowUpStatus.DueSoon,
            "missed" => FollowUpStatus.Missed,
            "overdue" => FollowUpStatus.Overdue,
            _ => null
        };

        return status.HasValue;
    }
}
