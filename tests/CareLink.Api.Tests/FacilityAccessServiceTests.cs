using System.Security.Claims;
using CareLink.Api.Security;

namespace CareLink.Api.Tests;

public sealed class FacilityAccessServiceTests
{
    private readonly FacilityAccessService _service = new();

    [Fact]
    public void Manager_can_access_any_facility()
    {
        var user = CreateUser(CareLinkRoles.Manager);

        Assert.True(_service.CanAccess(user, "FAC-9999"));
    }

    [Fact]
    public void Clinic_staff_can_access_their_assigned_facility()
    {
        var user = CreateUser(CareLinkRoles.ClinicStaff, "FAC-0101");

        Assert.True(_service.CanAccess(user, "FAC-0101"));
    }

    [Fact]
    public void Clinic_staff_cannot_access_another_facility()
    {
        var user = CreateUser(CareLinkRoles.ClinicStaff, "FAC-0101");

        Assert.False(_service.CanAccess(user, "FAC-0202"));
    }

    private static ClaimsPrincipal CreateUser(string role, string? facilityId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user"),
            new(ClaimTypes.Role, role)
        };
        if (facilityId is not null)
        {
            claims.Add(new Claim("facility_id", facilityId));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
